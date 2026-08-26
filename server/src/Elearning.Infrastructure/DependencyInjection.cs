using Elearning.Infrastructure.DependencyInjectionModules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elearning.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDatabaseInfrastructure(configuration);
        services.AddIdentityInfrastructure();
        services.AddJwtAuthenticationInfrastructure(configuration, environment);
        services.AddAuthenticationModule();
        services.AddFeatureModules();
        services.AddSecurityInfrastructure();
        services.AddHealthInfrastructure();
        return services;
    }
}
