using Elearning.Application.Auth.Models;

namespace Elearning.Application.Auth.Ports;

public interface IRefreshSessionRotator
{
    Task<AuthSessionResult> RotateAsync(string refreshToken, CancellationToken cancellationToken);
}
