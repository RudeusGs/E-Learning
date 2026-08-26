using Elearning.Api.DependencyInjectionModules;

namespace Elearning.Api.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddApiPresentation();
        services.AddApiAuthorization();
        services.AddProxyForwarding(configuration);
        services.AddApiRateLimiting(configuration);
        return services;
    }
}
