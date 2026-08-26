using Elearning.Application.Errors;

namespace Elearning.Application.Exceptions;

public sealed class AuthorizationException : AppProblemException
{
    private AuthorizationException(string code, string title, string? detail = null)
        : base(403, code, title, detail)
    {
    }

    public static AuthorizationException AccountDisabled() =>
        new(ErrorCodes.AccountDisabled, "Account is disabled");

    public static AuthorizationException LessonLocked() =>
        new(
            ErrorCodes.LessonLocked,
            "Lesson is locked",
            "Complete the previous published lesson first.");
}
