namespace Elearning.Application.Auth;

public interface IGetCurrentUserQueryHandler
{
    Task<UserDto> ExecuteAsync(GetCurrentUserQuery query, CancellationToken cancellationToken);
}
