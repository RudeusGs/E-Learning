namespace Elearning.Application.Auth;

public sealed record LogoutCommand(
    string? RefreshToken,
    string? AccessTokenJti,
    long? AccessTokenUserId);
