using Elearning.Application.Auth.Models;
using Elearning.Application.Auth.Ports;
using Elearning.Application.Auth.Security;
using Elearning.Application.Exceptions;

namespace Elearning.Application.Auth;

public sealed class RefreshSessionCommandHandler(
    IRefreshSessionRotator rotator,
    ISecurityAuditWriter auditWriter) : IRefreshSessionCommandHandler
{
    public async Task<AuthSessionResult> ExecuteAsync(
        RefreshSessionCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            return await rotator.RotateAsync(command.RefreshToken ?? string.Empty, cancellationToken);
        }
        catch (RefreshTokenReplayException exception)
        {
            auditWriter.RefreshTokenReplayDetected(exception.UserId);
            auditWriter.RefreshFailed(exception.UserId, exception.FailureReason);
            throw;
        }
        catch (AuthenticationException exception)
        {
            auditWriter.RefreshFailed(exception.UserId, exception.FailureReason);
            throw;
        }
    }
}
