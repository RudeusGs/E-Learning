using Elearning.Application.Auth.Security;
using Microsoft.Extensions.Logging;

namespace Elearning.Infrastructure.Auth;

public sealed partial class SecurityAuditLogger(ILogger<SecurityAuditLogger> logger) : ISecurityAuditWriter
{
    public void LoginSucceeded(long userId) =>
        LogLoginSucceeded(logger, userId);

    public void LoginFailed(long? userId, AuthenticationFailureReason reason) =>
        LogLoginFailed(logger, userId, reason);

    public void RefreshFailed(long? userId, AuthenticationFailureReason reason) =>
        LogRefreshFailed(logger, userId, reason);

    public void RefreshTokenReplayDetected(long userId) =>
        LogRefreshReplayDetected(logger, userId);

    public void LogoutCompleted(long? userId, bool sessionFound) =>
        LogLogoutCompleted(logger, userId, sessionFound);

    public void AccountDisabled(long actorUserId, long targetUserId) =>
        LogAccountDisabled(logger, actorUserId, targetUserId);

    [LoggerMessage(
        EventId = SecurityAuditEventIds.LoginSucceeded,
        Level = LogLevel.Information,
        Message = "Authentication login succeeded for user {UserId}")]
    private static partial void LogLoginSucceeded(ILogger logger, long userId);

    [LoggerMessage(
        EventId = SecurityAuditEventIds.LoginFailed,
        Level = LogLevel.Warning,
        Message = "Authentication login failed for user {UserId}; reason {Reason}")]
    private static partial void LogLoginFailed(
        ILogger logger,
        long? userId,
        AuthenticationFailureReason reason);

    [LoggerMessage(
        EventId = SecurityAuditEventIds.RefreshFailed,
        Level = LogLevel.Information,
        Message = "Authentication refresh failed for user {UserId}; reason {Reason}")]
    private static partial void LogRefreshFailed(
        ILogger logger,
        long? userId,
        AuthenticationFailureReason reason);

    [LoggerMessage(
        EventId = SecurityAuditEventIds.RefreshTokenReplayDetected,
        Level = LogLevel.Warning,
        Message = "Authentication refresh-token replay detected for user {UserId}; token family revoked")]
    private static partial void LogRefreshReplayDetected(ILogger logger, long userId);

    [LoggerMessage(
        EventId = SecurityAuditEventIds.LogoutCompleted,
        Level = LogLevel.Information,
        Message = "Authentication logout completed for user {UserId}; server session found: {SessionFound}")]
    private static partial void LogLogoutCompleted(ILogger logger, long? userId, bool sessionFound);

    [LoggerMessage(
        EventId = SecurityAuditEventIds.AccountDisabled,
        Level = LogLevel.Warning,
        Message = "Admin user {ActorUserId} disabled student account {TargetUserId} and revoked its authentication sessions")]
    private static partial void LogAccountDisabled(ILogger logger, long actorUserId, long targetUserId);
}
