using System.Security.Claims;
using Elearning.Application.Exceptions;

namespace Elearning.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static long GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(value, out var userId) || userId <= 0)
        {
            throw AuthenticationException.Unauthenticated();
        }

        return userId;
    }
}
