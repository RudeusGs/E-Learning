using Elearning.Domain;

namespace Elearning.Application.Auth;

public sealed record UserDto(long Id, string FullName, string Email, UserRole Role);
