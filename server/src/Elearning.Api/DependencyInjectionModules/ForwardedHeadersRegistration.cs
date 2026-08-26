using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Elearning.Api.DependencyInjectionModules;

internal static class ForwardedHeadersRegistration
{
    public static IServiceCollection AddProxyForwarding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            foreach (var value in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
            {
                if (!IPAddress.TryParse(value, out var address))
                {
                    throw new InvalidOperationException(
                        $"ReverseProxy:KnownProxies contains invalid IP address '{value}'.");
                }

                options.KnownProxies.Add(address);
            }

            foreach (var value in configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
            {
                if (!System.Net.IPNetwork.TryParse(value, out var network))
                {
                    throw new InvalidOperationException(
                        $"ReverseProxy:KnownNetworks contains invalid CIDR network '{value}'.");
                }

                options.KnownIPNetworks.Add(network);
            }
        });
        return services;
    }
}
