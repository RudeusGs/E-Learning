using Elearning.Application.Auth.Models;

namespace Elearning.Application.Auth;

public interface ILoginCommandHandler
{
    Task<AuthSessionResult> ExecuteAsync(LoginCommand command, CancellationToken cancellationToken);
}
