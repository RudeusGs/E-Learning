namespace Elearning.Application.Auth.Security;

public interface ISecurityAuditWriter
{
    void LoginSucceeded(long userId);
    void LoginFailed(long? userId, AuthenticationFailureReason reason);
    void RefreshFailed(long? userId, AuthenticationFailureReason reason);
    void RefreshTokenReplayDetected(long userId);
    void LogoutCompleted(long? userId, bool sessionFound);
    void AccountDisabled(long actorUserId, long targetUserId);
}
