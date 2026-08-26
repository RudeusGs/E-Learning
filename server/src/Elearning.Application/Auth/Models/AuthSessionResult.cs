namespace Elearning.Application.Auth.Models;

public sealed record AuthSessionResult(
    UserDto User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
