namespace Elearning.Application.Auth;

public sealed record LoginCommand(string? Email, string? Password);
