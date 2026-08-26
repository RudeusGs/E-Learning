using Elearning.Application.Auth.Models;

namespace Elearning.Application.Auth.Ports;

public interface IIdentityAuthenticationGateway
{
    Task<CredentialAuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<AuthenticatedUser?> FindActiveUserAsync(long userId, CancellationToken cancellationToken);

    void EqualizeInvalidCredentialTiming(string providedPassword);
}
