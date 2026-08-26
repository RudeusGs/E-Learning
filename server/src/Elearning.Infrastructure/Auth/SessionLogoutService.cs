using Elearning.Application.Auth;
using Elearning.Application.Auth.Models;
using Elearning.Application.Auth.Ports;
using Elearning.Application.Auth.Security;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Auth;

public sealed class SessionLogoutService(
    ElearningDbContext dbContext,
    SessionRevoker sessionRevoker) : ISessionLogoutService
{
    public async Task<SessionLogoutResult> LogoutAsync(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        var refreshUserId = await RevokeRefreshSessionAsync(command.RefreshToken, cancellationToken);
        var accessSessionFound = await BlacklistAccessSessionAsync(command, cancellationToken);
        return new SessionLogoutResult(
            refreshUserId ?? command.AccessTokenUserId,
            refreshUserId is not null || accessSessionFound);
    }

    private async Task<long?> RevokeRefreshSessionAsync(
        string? refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var tokenHash = TokenSecurity.HashRefreshToken(refreshToken);
        var storedRefresh = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (storedRefresh is null)
        {
            return null;
        }

        await sessionRevoker.RevokeFamilyAsync(
            storedRefresh.UserId,
            storedRefresh.FamilyId,
            SessionRevocationReason.Logout,
            cancellationToken);
        return storedRefresh.UserId;
    }

    private async Task<bool> BlacklistAccessSessionAsync(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.AccessTokenJti) || command.AccessTokenUserId is null)
        {
            return false;
        }

        var accessExpiry = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token =>
                token.UserId == command.AccessTokenUserId.Value &&
                token.AccessTokenJti == command.AccessTokenJti)
            .Select(token => (DateTimeOffset?)token.AccessTokenExpiresAtUtc)
            .SingleOrDefaultAsync(cancellationToken);
        if (accessExpiry is null)
        {
            return false;
        }

        await sessionRevoker.BlacklistAccessTokenAsync(
            command.AccessTokenUserId.Value,
            command.AccessTokenJti,
            accessExpiry.Value,
            SessionRevocationReason.Logout,
            cancellationToken);
        return true;
    }
}
