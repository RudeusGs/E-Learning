using Elearning.Infrastructure.Identity;

namespace Elearning.Infrastructure.Auth;

internal sealed class RefreshConsumeConflictException(RefreshToken? currentState) : Exception
{
    public RefreshToken? CurrentState { get; } = currentState;
}
