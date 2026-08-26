namespace Elearning.Application.Auth.Security;

public enum SessionRevocationReason
{
    Logout,
    AccountDisabled,
    AccountInactive,
    SecurityStampChanged,
    RefreshTokenReuseDetected
}
