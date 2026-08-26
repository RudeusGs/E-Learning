using Elearning.Application.Errors;

namespace Elearning.Application.Exceptions;

public sealed class RequestValidationException : AppProblemException
{
    public RequestValidationException(string title, string? detail = null)
        : this(ErrorCodes.ValidationFailed, title, detail)
    {
    }

    public RequestValidationException(string code, string title, string? detail = null)
        : base(400, code, title, detail)
    {
    }
}
