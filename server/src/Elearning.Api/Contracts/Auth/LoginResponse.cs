namespace Elearning.Api.Contracts.Auth;

public sealed record LoginResponse(
    UserResponse User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc);
