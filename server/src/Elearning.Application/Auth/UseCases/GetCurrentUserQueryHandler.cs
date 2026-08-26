using Elearning.Application.Auth.Ports;
using Elearning.Application.Exceptions;

namespace Elearning.Application.Auth;

public sealed class GetCurrentUserQueryHandler(IIdentityAuthenticationGateway identity)
    : IGetCurrentUserQueryHandler
{
    public async Task<UserDto> ExecuteAsync(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken)
    {
        var user = await identity.FindActiveUserAsync(query.UserId, cancellationToken);
        return user?.User ?? throw AuthenticationException.Unauthenticated();
    }
}
