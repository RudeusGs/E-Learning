using Elearning.Domain;

namespace Elearning.Infrastructure.Identity;

public static class UserRoleExtensions
{
    public static string ToIdentityName(this UserRole role) => role.ToString();

    public static UserRole ToUserRole(this string roleName)
    {
        if (Enum.TryParse<UserRole>(roleName, true, out var role))
        {
            return role;
        }

        throw new InvalidOperationException($"Unknown persisted role '{roleName}'.");
    }
}
