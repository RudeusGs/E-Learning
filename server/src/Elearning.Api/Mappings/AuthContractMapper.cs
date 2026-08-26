using Elearning.Api.Contracts.Auth;
using Elearning.Application.Auth;
using Elearning.Application.Auth.Models;

namespace Elearning.Api.Mappings;

public static class AuthContractMapper
{
    public static LoginCommand ToCommand(this LoginRequest request) =>
        new(request.Email, request.Password);

    public static LoginResponse ToResponse(this AuthSessionResult session) =>
        new(session.User.ToResponse(), session.AccessToken, session.AccessTokenExpiresAtUtc);

    public static UserResponse ToResponse(this UserDto user) =>
        new(user.Id, user.FullName, user.Email, user.Role);
}
