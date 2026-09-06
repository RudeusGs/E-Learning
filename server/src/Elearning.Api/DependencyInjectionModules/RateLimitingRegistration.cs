using System.Threading.RateLimiting;
using Elearning.Api.Errors;
using Elearning.Api.Security;
using Elearning.Application.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elearning.Api.DependencyInjectionModules;

internal static class RateLimitingRegistration
{
    private const string UnknownIpPartition = "unknown";

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            AddFixedWindowIpPolicy(
                options,
                RateLimitPolicyNames.Authentication,
                configuration.GetValue("RateLimiting:AuthenticationPermitLimit", 10),
                TimeSpan.FromMinutes(1));
            AddFixedWindowIpPolicy(
                options,
                RateLimitPolicyNames.AuthenticationSession,
                configuration.GetValue("RateLimiting:RefreshPermitLimit", 30),
                TimeSpan.FromMinutes(1));
            AddFixedWindowUserPolicy(
                options,
                RateLimitPolicyNames.StudentInteraction,
                configuration.GetValue("RateLimiting:StudentInteractionPermitLimit", 120),
                TimeSpan.FromMinutes(1));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetConcurrencyLimiter(
                    partitionKey: "global",
                    factory: _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = configuration.GetValue("RateLimiting:GlobalConcurrencyLimit", 100),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = configuration.GetValue("RateLimiting:GlobalQueueLimit", 50)
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Type = ProblemDetailsMetadata.RateLimitedType,
                    Title = "Too many requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Extensions =
                    {
                        [ProblemDetailsMetadata.CodeExtension] = ErrorCodes.RateLimited,
                        [ProblemDetailsMetadata.TraceIdExtension] = context.HttpContext.TraceIdentifier
                    }
                }, cancellationToken);
            };
        });
        return services;
    }

    private static void AddFixedWindowIpPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        TimeSpan window)
    {
        if (permitLimit < 1)
        {
            throw new InvalidOperationException($"Rate limiter '{policyName}' permit limit must be positive.");
        }

        options.AddPolicy(policyName, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? UnknownIpPartition,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }
    private static void AddFixedWindowUserPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        TimeSpan window)
    {
        if (permitLimit < 1)
        {
            throw new InvalidOperationException($"Rate limiter '{policyName}' permit limit must be positive.");
        }

        options.AddPolicy(policyName, context =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var partition = string.IsNullOrWhiteSpace(userId)
                ? context.Connection.RemoteIpAddress?.ToString() ?? UnknownIpPartition
                : $"user:{userId}";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partition,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
    }

}