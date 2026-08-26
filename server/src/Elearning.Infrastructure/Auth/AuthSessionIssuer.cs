using Elearning.Application.Auth.Models;
using Elearning.Application.Auth.Ports;
using Elearning.Infrastructure.Identity;
using Elearning.Infrastructure.Persistence;

namespace Elearning.Infrastructure.Auth;

public sealed class AuthSessionIssuer(
    ElearningDbContext dbContext,
    JwtAccessTokenGenerator accessTokenGenerator,
    JwtOptions options,
    TimeProvider timeProvider) : IAuthSessionIssuer
{
    public async Task<AuthSessionResult> IssueAsync(
        AuthenticatedUser user,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var refreshExpiresAtUtc = now + options.RefreshTokenLifetime;
        var access = accessTokenGenerator.Generate(user, now);
        var refreshToken = TokenSecurity.GenerateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = TokenSecurity.HashRefreshToken(refreshToken),
            FamilyId = Guid.NewGuid(),
            SecurityStampHash = user.SecurityStampHash,
            AccessTokenJti = access.Jti,
            AccessTokenExpiresAtUtc = access.ExpiresAtUtc,
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshExpiresAtUtc,
            UserId = user.User.Id
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthSessionResult(
            user.User,
            access.Token,
            access.ExpiresAtUtc,
            refreshToken,
            refreshExpiresAtUtc);
    }
}
