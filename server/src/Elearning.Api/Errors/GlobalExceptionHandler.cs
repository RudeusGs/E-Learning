using Elearning.Application;
using Elearning.Application.Errors;
using Elearning.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Api.Errors;

public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, code, title, detail, logAsError) = exception switch
        {
            AppProblemException problem => (problem.Status, problem.Code, problem.Title, problem.Detail, false),
            DomainValidationException validation => (400, ErrorCodes.ValidationFailed, ProblemDetailsMetadata.ValidationFailedTitle, validation.Message, false),
            DbUpdateConcurrencyException => (409, ErrorCodes.ConcurrencyConflict, "Concurrency conflict", "The resource was changed by another request. Reload it and retry.", false),
            _ => (500, ErrorCodes.InternalError, "An unexpected error occurred", "Use the traceId when contacting support.", true)
        };

        if (logAsError)
        {
            LogUnhandledFailure(logger, exception, httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Type = $"{ProblemDetailsMetadata.ErrorTypeBaseUri}{code.ToLowerInvariant().Replace('_', '-')}",
                Title = title,
                Detail = detail,
                Status = status,
                Extensions =
                {
                    [ProblemDetailsMetadata.CodeExtension] = code,
                    [ProblemDetailsMetadata.TraceIdExtension] = httpContext.TraceIdentifier
                }
            },
            Exception = exception
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled request failure with trace id {TraceId}")]
    private static partial void LogUnhandledFailure(ILogger logger, Exception exception, string traceId);
}
