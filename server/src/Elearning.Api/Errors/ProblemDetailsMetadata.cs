namespace Elearning.Api.Errors;

internal static class ProblemDetailsMetadata
{
    public const string CodeExtension = "code";
    public const string TraceIdExtension = "traceId";
    public const string ErrorTypeBaseUri = "https://elearning.local/errors/";
    public const string HttpErrorType = $"{ErrorTypeBaseUri}http-error";
    public const string RateLimitedType = $"{ErrorTypeBaseUri}rate-limited";
    public const string ValidationFailedType = $"{ErrorTypeBaseUri}validation-failed";
    public const string UnauthenticatedType = $"{ErrorTypeBaseUri}unauthenticated";
    public const string ForbiddenType = $"{ErrorTypeBaseUri}forbidden";
    public const string ValidationFailedTitle = "Validation failed";
}
