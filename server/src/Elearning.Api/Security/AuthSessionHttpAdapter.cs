using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Elearning.Api.Contracts.Auth;
using Elearning.Api.Mappings;
using Elearning.Application.Auth;
using Elearning.Application.Auth.Models;

namespace Elearning.Api.Security;

public sealed class AuthSessionHttpAdapter(RefreshTokenCookie refreshTokenCookie)
{
    public RefreshSessionCommand CreateRefreshCommand(HttpRequest request) =>
        new(refreshTokenCookie.Read(request));

    public LogoutCommand CreateLogoutCommand(HttpRequest request, ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return new LogoutCommand(refreshTokenCookie.Read(request), null, null);
        }

        var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        return new LogoutCommand(
            refreshTokenCookie.Read(request),
            jti,
            principal.GetRequiredUserId());
    }

    public LoginResponse WriteSession(HttpResponse response, AuthSessionResult session)
    {
        refreshTokenCookie.Write(response, session.RefreshToken, session.RefreshTokenExpiresAtUtc);
        return session.ToResponse();
    }

    public void DeleteSessionCookie(HttpResponse response) =>
        refreshTokenCookie.Delete(response);
}
