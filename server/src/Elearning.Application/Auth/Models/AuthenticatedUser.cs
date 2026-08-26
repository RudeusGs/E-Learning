namespace Elearning.Application.Auth.Models;

public sealed record AuthenticatedUser(
    UserDto User,
    string UserName,
    string IdentityRole,
    string SecurityStampHash);
