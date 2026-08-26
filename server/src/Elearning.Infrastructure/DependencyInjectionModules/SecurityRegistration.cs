using Elearning.Application.Security;
using Elearning.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Elearning.Infrastructure.DependencyInjectionModules;

internal static class SecurityRegistration
{
    public static IServiceCollection AddSecurityInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IContentSanitizer, HtmlContentSanitizer>();
        services.AddSingleton<IVideoNormalizer, YouTubeVideoNormalizer>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
