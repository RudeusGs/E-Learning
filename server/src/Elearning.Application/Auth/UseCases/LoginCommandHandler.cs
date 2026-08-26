using Elearning.Application.Auth.Models;
using Elearning.Application.Auth.Ports;
using Elearning.Application.Auth.Security;
using Elearning.Application.Exceptions;

namespace Elearning.Application.Auth;

public sealed class LoginCommandHandler(
    IIdentityAuthenticationGateway identity,
    IAuthSessionIssuer sessionIssuer,
    ISecurityAuditWriter auditWriter) : ILoginCommandHandler
{
    public async Task<AuthSessionResult> ExecuteAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) ||
            command.Email.Length > AuthValidationLimits.MaximumEmailLength ||
            string.IsNullOrWhiteSpace(command.Password) ||
            command.Password.Length > AuthValidationLimits.MaximumPasswordLength)
        {
            identity.EqualizeInvalidCredentialTiming(command.Password ?? string.Empty);
            auditWriter.LoginFailed(userId: null, AuthenticationFailureReason.InvalidInput);
            throw AuthenticationException.InvalidCredentials();
        }

        var authentication = await identity.AuthenticateAsync(
            command.Email,
            command.Password,
            cancellationToken);

        if (!authentication.Succeeded)
        {
            auditWriter.LoginFailed(
                authentication.UserId,
                authentication.FailureReason ?? AuthenticationFailureReason.InvalidCredentials);
            throw AuthenticationException.InvalidCredentials(authentication.UserId);
        }

        var session = await sessionIssuer.IssueAsync(authentication.User!, cancellationToken);
        auditWriter.LoginSucceeded(authentication.User!.User.Id);
        return session;
    }
}
