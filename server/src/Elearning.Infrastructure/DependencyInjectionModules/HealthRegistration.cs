using Elearning.Infrastructure.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elearning.Infrastructure.DependencyInjectionModules;

internal static class HealthRegistration
{
    public static IServiceCollection AddHealthInfrastructure(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [HealthCheckTags.Live])
            .AddCheck<PostgresReadinessHealthCheck>("postgres", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
