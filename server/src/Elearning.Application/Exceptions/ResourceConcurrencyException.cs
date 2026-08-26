using Elearning.Application.Errors;

namespace Elearning.Application.Exceptions;

public sealed class ResourceConcurrencyException(string resourceName)
    : AppProblemException(
        409,
        ErrorCodes.ConcurrencyConflict,
        "Concurrency conflict",
        $"The {resourceName.ToLowerInvariant()} was changed by another request. Reload it and retry.")
{
}
