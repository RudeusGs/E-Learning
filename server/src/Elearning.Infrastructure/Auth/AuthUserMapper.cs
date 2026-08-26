using Elearning.Application.Auth;
using Elearning.Application.Auth.Models;
using Elearning.Infrastructure.Identity;

namespace Elearning.Infrastructure.Auth;

internal static class AuthUserMapper
{
    public static AuthenticatedUser Map(ApplicationUser user, string identityRole) =>
        new(
            new UserDto(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                identityRole.ToUserRole()),
            user.UserName ?? string.Empty,
            identityRole,
            TokenSecurity.HashSecurityStamp(user.SecurityStamp));
}
