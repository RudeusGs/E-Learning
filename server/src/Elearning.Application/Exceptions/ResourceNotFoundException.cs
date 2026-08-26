using Elearning.Application.Errors;

namespace Elearning.Application.Exceptions;

public sealed class ResourceNotFoundException(string resourceName)
    : AppProblemException(404, ErrorCodes.ResourceNotFound, $"{resourceName} not found")
{
}
