using Elearning.Application.Auth.Security;
using Elearning.Infrastructure.Identity;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Auth;

public sealed class SessionRevoker(
    ElearningDbContext dbContext,
    TimeProvider timeProvider)
{
    public Task RevokeFamilyAsync(
        long userId,
        Guid familyId,
        SessionRevocationReason reason,
        CancellationToken cancellationToken) =>
        RevokeAsync(
            token => token.UserId == userId && token.FamilyId == familyId,
            userId,
            reason,
            familyId,
            cancellationToken);

    public Task RevokeAllForUserAsync(
        long userId,
        SessionRevocationReason reason,
        CancellationToken cancellationToken) =>
        RevokeAsync(
            token => token.UserId == userId,
            userId,
            reason,
            familyId: null,
            cancellationToken);

    public async Task BlacklistAccessTokenAsync(
        long userId,
        string jti,
        DateTimeOffset expiresAtUtc,
        SessionRevocationReason reason,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (string.IsNullOrWhiteSpace(jti) || expiresAtUtc <= now)
        {
            return;
        }

        await InsertBlacklistAsync(
            userId,
            jti,
            expiresAtUtc,
            now,
            reason.ToStorageValue(),
            cancellationToken);
    }

    private async Task RevokeAsync(
        System.Linq.Expressions.Expression<Func<RefreshToken, bool>> predicate,
        long userId,
        SessionRevocationReason reason,
        Guid? familyId,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(() => RevokeWithinTransactionAsync(
            predicate,
            userId,
            reason,
            familyId,
            cancellationToken));
    }

    private async Task RevokeWithinTransactionAsync(
        System.Linq.Expressions.Expression<Func<RefreshToken, bool>> predicate,
        long userId,
        SessionRevocationReason reason,
        Guid? familyId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AcquireFamilyLocksAsync(predicate, familyId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var reasonValue = reason.ToStorageValue();
        var accessTokens = await LoadActiveAccessTokensAsync(predicate, now, cancellationToken);
        await MarkRefreshTokensRevokedAsync(predicate, now, reasonValue, cancellationToken);
        await BlacklistAccessTokensAsync(userId, accessTokens, now, reasonValue, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task AcquireFamilyLocksAsync(
        System.Linq.Expressions.Expression<Func<RefreshToken, bool>> predicate,
        Guid? familyId,
        CancellationToken cancellationToken)
    {
        if (familyId is not null)
        {
            await RefreshTokenFamilyLock.AcquireAsync(dbContext, familyId.Value, cancellationToken);
            return;
        }

        var familyIds = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(predicate)
            .Select(token => token.FamilyId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var currentFamilyId in familyIds.Order())
        {
            await RefreshTokenFamilyLock.AcquireAsync(dbContext, currentFamilyId, cancellationToken);
        }
    }

    private Task<List<AccessTokenDescriptor>> LoadActiveAccessTokensAsync(
        System.Linq.Expressions.Expression<Func<RefreshToken, bool>> predicate,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .AsNoTracking()
            .Where(predicate)
            .Where(token => token.AccessTokenExpiresAtUtc > now)
            .Select(token => new AccessTokenDescriptor(token.AccessTokenJti, token.AccessTokenExpiresAtUtc))
            .ToListAsync(cancellationToken);

    private Task<int> MarkRefreshTokensRevokedAsync(
        System.Linq.Expressions.Expression<Func<RefreshToken, bool>> predicate,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(predicate)
            .Where(token => token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAtUtc, now)
                .SetProperty(token => token.RevocationReason, reason), cancellationToken);

    private async Task BlacklistAccessTokensAsync(
        long userId,
        IEnumerable<AccessTokenDescriptor> accessTokens,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken)
    {
        foreach (var token in accessTokens)
        {
            await InsertBlacklistAsync(userId, token.Jti, token.ExpiresAtUtc, now, reason, cancellationToken);
        }
    }

    private Task<int> InsertBlacklistAsync(
        long userId,
        string jti,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset revokedAtUtc,
        string reason,
        CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "BlacklistedTokens" ("TokenId", "UserId", "ExpiresAtUtc", "RevokedAtUtc", "Reason")
            VALUES ({jti}, {userId}, {expiresAtUtc}, {revokedAtUtc}, {reason})
            ON CONFLICT ("TokenId") DO NOTHING;
            """, cancellationToken);

    private sealed record AccessTokenDescriptor(string Jti, DateTimeOffset ExpiresAtUtc);
}
