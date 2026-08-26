namespace Elearning.Application.Auth.Security;

public enum AuthenticationFailureReason
{
    InvalidInput,
    InvalidCredentials,
    LockedOut,
    Unauthenticated,
    InvalidRefreshToken,
    RefreshTokenExpired,
    RefreshRetryRequired,
    RefreshTokenReuseDetected,
    AccountInactive,
    SecurityStampChanged,
    InvalidRoleState
}
