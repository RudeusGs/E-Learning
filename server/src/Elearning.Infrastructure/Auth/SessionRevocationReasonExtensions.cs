using Elearning.Application.Auth.Security;

namespace Elearning.Infrastructure.Auth;

internal static class SessionRevocationReasonExtensions
{
    public static string ToStorageValue(this SessionRevocationReason reason) => reason switch
    {
        SessionRevocationReason.Logout => "LOGOUT",
        SessionRevocationReason.AccountDisabled => "ACCOUNT_DISABLED",
        SessionRevocationReason.AccountInactive => "ACCOUNT_INACTIVE",
        SessionRevocationReason.SecurityStampChanged => "SECURITY_STAMP_CHANGED",
        SessionRevocationReason.RefreshTokenReuseDetected => "REFRESH_REUSE_DETECTED",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown session revocation reason.")
    };
}
