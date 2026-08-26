namespace Elearning.Application.Auth.Security;

public static class SecurityAuditEventIds
{
    public const int LoginSucceeded = 1001;
    public const int LoginFailed = 1002;
    public const int RefreshFailed = 1003;
    public const int RefreshTokenReplayDetected = 1004;
    public const int LogoutCompleted = 1005;
    public const int AccountDisabled = 1006;
}
