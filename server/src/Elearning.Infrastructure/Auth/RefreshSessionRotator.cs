using Elearning.Application.Auth.Models;
using Elearning.Application.Auth.Ports;
using Elearning.Application.Auth.Security;
using Elearning.Application.Exceptions;
using Elearning.Infrastructure.Identity;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Auth;

public sealed class RefreshSessionRotator(
    ElearningDbContext dbContext,
    IIdentityAuthenticationGateway identity,
    JwtAccessTokenGenerator accessTokenGenerator,
    SessionRevoker sessionRevoker,
    JwtOptions options,
    TimeProvider timeProvider) : IRefreshSessionRotator
{
    public async Task<AuthSessionResult> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw AuthenticationException.InvalidRefreshToken();
        }

        var now = timeProvider.GetUtcNow();
        var existing = await FindTokenAsync(refreshToken, cancellationToken);
        await EnsureCanRotateAsync(existing, now, cancellationToken);
        var user = await FindValidUserAsync(existing, cancellationToken);
        return await RotateAtomicallyAsync(existing, user, now, cancellationToken);
    }

    private async Task<RefreshToken> FindTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = TokenSecurity.HashRefreshToken(refreshToken);
        return await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken)
            ?? throw AuthenticationException.InvalidRefreshToken();
    }

    private async Task<AuthenticatedUser> FindValidUserAsync(
        RefreshToken existing,
        CancellationToken cancellationToken)
    {
        var user = await identity.FindActiveUserAsync(existing.UserId, cancellationToken);
        if (user is null)
        {
            await sessionRevoker.RevokeFamilyAsync(
                existing.UserId,
                existing.FamilyId,
                SessionRevocationReason.AccountInactive,
                cancellationToken);
            throw AuthenticationException.InvalidRefreshToken(
                existing.UserId,
                AuthenticationFailureReason.AccountInactive);
        }

        if (!string.Equals(existing.SecurityStampHash, user.SecurityStampHash, StringComparison.Ordinal))
        {
            await sessionRevoker.RevokeFamilyAsync(
                existing.UserId,
                existing.FamilyId,
                SessionRevocationReason.SecurityStampChanged,
                cancellationToken);
            throw AuthenticationException.SessionRevoked(
                existing.UserId,
                AuthenticationFailureReason.SecurityStampChanged);
        }

        return user;
    }

    private async Task<AuthSessionResult> RotateAtomicallyAsync(
        RefreshToken existing,
        AuthenticatedUser user,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var replacement = new RefreshReplacement(Guid.NewGuid(), TokenSecurity.GenerateRefreshToken());
        var access = accessTokenGenerator.Generate(user, now);
        var strategy = dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync(() => ConsumeAndReplaceAsync(
                existing,
                user,
                access,
                replacement,
                now,
                cancellationToken));
        }
        catch (RefreshConsumeConflictException exception)
        {
            if (exception.CurrentState is null)
            {
                throw AuthenticationException.InvalidRefreshToken(existing.UserId);
            }

            await EnsureCanRotateAsync(exception.CurrentState, now, cancellationToken);
            throw AuthenticationException.InvalidRefreshToken(existing.UserId);
        }
    }

    private async Task<AuthSessionResult> ConsumeAndReplaceAsync(
        RefreshToken existing,
        AuthenticatedUser user,
        AccessToken access,
        RefreshReplacement replacement,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await RefreshTokenFamilyLock.AcquireAsync(dbContext, existing.FamilyId, cancellationToken);
        if (!await TryConsumeAsync(existing, replacement.Id, now, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new RefreshConsumeConflictException(await LoadCurrentStateAsync(existing.Id, cancellationToken));
        }

        dbContext.RefreshTokens.Add(CreateReplacement(existing, user, access, replacement, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AuthSessionResult(
            user.User,
            access.Token,
            access.ExpiresAtUtc,
            replacement.Token,
            existing.ExpiresAtUtc);
    }

    private async Task<bool> TryConsumeAsync(
        RefreshToken existing,
        Guid replacementId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await dbContext.RefreshTokens
            .Where(token =>
                token.Id == existing.Id &&
                token.UsedAtUtc == null &&
                token.RevokedAtUtc == null &&
                token.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.UsedAtUtc, now)
                .SetProperty(token => token.ReplacedByTokenId, replacementId), cancellationToken) == 1;

    private Task<RefreshToken?> LoadCurrentStateAsync(Guid tokenId, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.Id == tokenId, cancellationToken);

    private static RefreshToken CreateReplacement(
        RefreshToken existing,
        AuthenticatedUser user,
        AccessToken access,
        RefreshReplacement replacement,
        DateTimeOffset now) => new()
        {
            Id = replacement.Id,
            TokenHash = TokenSecurity.HashRefreshToken(replacement.Token),
            FamilyId = existing.FamilyId,
            SecurityStampHash = user.SecurityStampHash,
            AccessTokenJti = access.Jti,
            AccessTokenExpiresAtUtc = access.ExpiresAtUtc,
            CreatedAtUtc = now,
            ExpiresAtUtc = existing.ExpiresAtUtc,
            UserId = user.User.Id
        };

    private async Task EnsureCanRotateAsync(
        RefreshToken token,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (token.ExpiresAtUtc <= now)
        {
            throw AuthenticationException.RefreshTokenExpired(token.UserId);
        }

        if (token.RevokedAtUtc is not null)
        {
            throw AuthenticationException.InvalidRefreshToken(token.UserId);
        }

        if (token.UsedAtUtc is null)
        {
            return;
        }

        if (token.ReplacedByTokenId is not null && now - token.UsedAtUtc.Value <= options.ConcurrentRefreshGrace)
        {
            throw AuthenticationException.RefreshRetryRequired(token.UserId);
        }

        await sessionRevoker.RevokeFamilyAsync(
            token.UserId,
            token.FamilyId,
            SessionRevocationReason.RefreshTokenReuseDetected,
            cancellationToken);
        throw new RefreshTokenReplayException(token.UserId);
    }

    private sealed record RefreshReplacement(Guid Id, string Token);
}
