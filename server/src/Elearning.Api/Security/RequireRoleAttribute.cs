using Elearning.Domain;
using Microsoft.AspNetCore.Authorization;

namespace Elearning.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireRoleAttribute : AuthorizeAttribute
{
    public RequireRoleAttribute(UserRole role)
    {
        Roles = role.ToString();
    }
}
