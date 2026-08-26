using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Elearning.Application.Auth.Models;
using Elearning.Application.Auth.Security;
using Microsoft.IdentityModel.Tokens;

namespace Elearning.Infrastructure.Auth;

public sealed class JwtAccessTokenGenerator(JwtOptions options)
{
    public AccessToken Generate(AuthenticatedUser user, DateTimeOffset issuedAtUtc)
    {
        var jti = Guid.NewGuid().ToString("N");
        var expiresAtUtc = issuedAtUtc + options.AccessTokenLifetime;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var userId = user.User.Id.ToString(CultureInfo.InvariantCulture);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.User.Email),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(AuthClaimNames.SessionStamp, user.SecurityStampHash),
            new(ClaimTypes.Role, user.IdentityRole)
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: issuedAtUtc.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), jti, expiresAtUtc);
    }
}
