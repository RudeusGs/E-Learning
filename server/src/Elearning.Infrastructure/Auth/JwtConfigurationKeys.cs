namespace Elearning.Infrastructure.Auth;

internal static class JwtConfigurationKeys
{
    public const string Issuer = "Jwt:Issuer";
    public const string Audience = "Jwt:Audience";
    public const string SigningKey = "Jwt:Key";
    public const string AccessTokenMinutes = "Jwt:AccessTokenMinutes";
    public const string RefreshTokenDays = "Jwt:RefreshTokenDays";
    public const string ClockSkewSeconds = "Jwt:ClockSkewSeconds";
    public const string ConcurrentRefreshGraceSeconds = "Jwt:ConcurrentRefreshGraceSeconds";
}
