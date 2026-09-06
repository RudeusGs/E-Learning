namespace Elearning.Api.Security;

public static class RateLimitPolicyNames
{
    public const string Authentication = "authentication";
    public const string AuthenticationSession = "auth-refresh";
    public const string StudentInteraction = "student-interaction";
}