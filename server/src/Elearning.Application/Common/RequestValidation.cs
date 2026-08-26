using Elearning.Application.Exceptions;

namespace Elearning.Application.Common;

public static class RequestValidation
{
    public const int DefaultLimit = 20;
    public const int MaximumLimit = 100;
    public const int MaximumSearchLength = 100;

    public static int ValidateLimit(int limit)
    {
        if (limit < 1 || limit > MaximumLimit)
        {
            throw new RequestValidationException(
                "Invalid pagination",
                "Limit must be between 1 and 100.");
        }

        return limit;
    }

    public static string? NormalizeSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var normalized = search.Trim();
        if (normalized.Length > MaximumSearchLength)
        {
            throw new RequestValidationException(
                "Invalid search",
                "Search cannot exceed 100 characters.");
        }

        return normalized;
    }
}
