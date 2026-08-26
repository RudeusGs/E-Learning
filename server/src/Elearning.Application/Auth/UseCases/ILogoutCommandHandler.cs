namespace Elearning.Application.Auth;

public interface ILogoutCommandHandler
{
    Task ExecuteAsync(LogoutCommand command, CancellationToken cancellationToken);
}
