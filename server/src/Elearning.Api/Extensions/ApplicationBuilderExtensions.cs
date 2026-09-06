using Elearning.Api.Contracts.Routing;
using Elearning.Api.Errors;
using Elearning.Application.Errors;
using Elearning.Infrastructure.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages(WriteAuthorizationProblemAsync);
        ConfigureEnvironmentMiddleware(app);
        ConfigureSecurityMiddleware(app);
        MapEndpoints(app);
        return app;
    }

    private static Task WriteAuthorizationProblemAsync(StatusCodeContext context)
    {
        var response = context.HttpContext.Response;
        return response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden
            ? response.WriteAsJsonAsync(CreateAuthorizationProblem(context.HttpContext))
            : Task.CompletedTask;
    }

    private static ProblemDetails CreateAuthorizationProblem(HttpContext context)
    {
        var unauthenticated = context.Response.StatusCode == StatusCodes.Status401Unauthorized;
        return new ProblemDetails
        {
            Type = unauthenticated
                ? ProblemDetailsMetadata.UnauthenticatedType
                : ProblemDetailsMetadata.ForbiddenType,
            Title = unauthenticated ? "Authentication required" : "Access forbidden",
            Status = context.Response.StatusCode,
            Extensions =
            {
                [ProblemDetailsMetadata.CodeExtension] = unauthenticated
                    ? ErrorCodes.Unauthenticated
                    : ErrorCodes.Forbidden,
                [ProblemDetailsMetadata.TraceIdExtension] = context.TraceIdentifier
            }
        };
    }

    private static void ConfigureEnvironmentMiddleware(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (app.Environment.IsProduction())
        {
            app.UseHsts();
        }
    }

    private static void ConfigureSecurityMiddleware(WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
    }

    private static void MapEndpoints(WebApplication app)
    {
        app.MapControllers();
        app.MapGet(SystemRoutes.Root, () => Results.Redirect(SystemRoutes.Swagger)).AllowAnonymous();
        MapHealthEndpoint(app, SystemRoutes.LiveHealth, HealthCheckTags.Live);
        MapHealthEndpoint(app, SystemRoutes.ReadyHealth, HealthCheckTags.Ready);
        MapHealthEndpoint(app, SystemRoutes.CompatibilityHealth, HealthCheckTags.Live);
    }

    private static void MapHealthEndpoint(WebApplication app, string route, string tag) =>
        app.MapHealthChecks(route, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(tag)
        }).AllowAnonymous();
}
