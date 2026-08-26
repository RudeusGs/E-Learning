using Elearning.Application.Auth;
using Elearning.Application.Auth.Models;

namespace Elearning.Application.Auth.Ports;

public interface ISessionLogoutService
{
    Task<SessionLogoutResult> LogoutAsync(LogoutCommand command, CancellationToken cancellationToken);
}
