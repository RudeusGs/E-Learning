using Elearning.Application.Auth.Security;
using Elearning.Application.Errors;

namespace Elearning.Application.Exceptions;

public sealed class AuthenticationException : AppProblemException
{
    private AuthenticationException(
        int status,
        string code,
        string title,
        AuthenticationFailureReason failureReason,
        long? userId = null,
        string? detail = null)
        : base(status, code, title, detail)
    {
        FailureReason = failureReason;
        UserId = userId;
    }

    public AuthenticationFailureReason FailureReason { get; }
    public long? UserId { get; }

    public static AuthenticationException InvalidCredentials(long? userId = null) =>
        new(
            401,
            ErrorCodes.InvalidCredentials,
            "Invalid credentials",
            AuthenticationFailureReason.InvalidCredentials,
            userId,
            "Email or password is incorrect.");

    public static AuthenticationException InvalidRefreshToken(
        long? userId = null,
        AuthenticationFailureReason failureReason = AuthenticationFailureReason.InvalidRefreshToken) =>
        new(
            401,
            ErrorCodes.InvalidRefreshToken,
            "Invalid refresh token",
            failureReason,
            userId);

    public static AuthenticationException RefreshTokenExpired(long userId) =>
        new(
            401,
            ErrorCodes.RefreshTokenExpired,
            "Session has expired",
            AuthenticationFailureReason.RefreshTokenExpired,
            userId);

    public static AuthenticationException RefreshRetryRequired(long userId) =>
        new(
            409,
            ErrorCodes.RefreshRetryRequired,
            "Refresh already completed",
            AuthenticationFailureReason.RefreshRetryRequired,
            userId,
            "Another browser request refreshed this session. Retry once using the latest refresh cookie.");

    public static AuthenticationException SessionRevoked(
        long userId,
        AuthenticationFailureReason failureReason) =>
        new(401, ErrorCodes.SessionRevoked, "Session has been revoked", failureReason, userId);

    public static AuthenticationException Unauthenticated() =>
        new(
            401,
            ErrorCodes.Unauthenticated,
            "Authentication required",
            AuthenticationFailureReason.Unauthenticated);

    public static AuthenticationException InvalidRoleState(long userId) =>
        new(
            403,
            ErrorCodes.InvalidRoleState,
            "Account role is invalid",
            AuthenticationFailureReason.InvalidRoleState,
            userId);
}
