using Elearning.Application.Auth.Security;
using Elearning.Application.Errors;

namespace Elearning.Application.Exceptions;

public sealed class RefreshTokenReplayException(long userId)
    : AppProblemException(
        401,
        ErrorCodes.RefreshTokenReuseDetected,
        "Session has been revoked",
        "A previously used refresh token was presented again.")
{
    public long UserId { get; } = userId;
    public AuthenticationFailureReason FailureReason { get; } =
        AuthenticationFailureReason.RefreshTokenReuseDetected;
}
