namespace Elearning.Application.Exceptions;

public sealed class ConflictException(
    string code,
    string title,
    string? detail = null) : AppProblemException(409, code, title, detail)
{
}
