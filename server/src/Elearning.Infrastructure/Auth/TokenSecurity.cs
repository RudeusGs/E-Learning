using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Elearning.Infrastructure.Auth;

internal static class TokenSecurity
{
    public static string GenerateRefreshToken() =>
        Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    public static string HashRefreshToken(string token) => Hash(token);

    public static string HashSecurityStamp(string? securityStamp) =>
        Hash(securityStamp ?? string.Empty);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
