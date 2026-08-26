using Elearning.Domain;

namespace Elearning.Api.Contracts.Auth;

public sealed record UserResponse(long Id, string FullName, string Email, UserRole Role);
