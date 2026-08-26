using Elearning.Application.Auth.Ports;
using Elearning.Application.Auth.Security;

namespace Elearning.Application.Auth;

public sealed class LogoutCommandHandler(
    ISessionLogoutService sessionLogout,
    ISecurityAuditWriter auditWriter) : ILogoutCommandHandler
{
    public async Task ExecuteAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var result = await sessionLogout.LogoutAsync(command, cancellationToken);
        auditWriter.LogoutCompleted(result.UserId, result.SessionFound);
    }
}
