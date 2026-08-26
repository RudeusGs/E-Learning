using Elearning.Application.Auth.Models;

namespace Elearning.Application.Auth.Ports;

public interface IAuthSessionIssuer
{
    Task<AuthSessionResult> IssueAsync(
        AuthenticatedUser user,
        CancellationToken cancellationToken);
}
