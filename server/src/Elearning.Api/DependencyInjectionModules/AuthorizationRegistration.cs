using Microsoft.AspNetCore.Authorization;

namespace Elearning.Api.DependencyInjectionModules;

internal static class AuthorizationRegistration
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        return services;
    }
}
