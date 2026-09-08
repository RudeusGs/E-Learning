using Elearning.Application.Common.Interfaces;
using Elearning.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elearning.Infrastructure.DependencyInjectionModules;

public static class CacheRegistration
{
    public static IServiceCollection AddCacheInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "Elearning_";
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
