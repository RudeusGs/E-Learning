using Elearning.Application.Auth.Models;

namespace Elearning.Application.Auth;

public interface IRefreshSessionCommandHandler
{
    Task<AuthSessionResult> ExecuteAsync(
        RefreshSessionCommand command,
        CancellationToken cancellationToken);
}
