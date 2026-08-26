using Elearning.Application.Auth.Security;

namespace Elearning.Application.Auth.Models;

public sealed record CredentialAuthenticationResult(
    AuthenticatedUser? User,
    long? UserId,
    AuthenticationFailureReason? FailureReason)
{
    public bool Succeeded => User is not null;
}
