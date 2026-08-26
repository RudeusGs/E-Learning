namespace Elearning.Infrastructure.Auth;

public sealed record AccessToken(string Token, string Jti, DateTimeOffset ExpiresAtUtc);
