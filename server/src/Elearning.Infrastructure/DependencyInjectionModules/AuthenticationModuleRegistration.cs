using Elearning.Application.Auth;
using Elearning.Application.Auth.Ports;
using Elearning.Application.Auth.Security;
using Elearning.Infrastructure.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Elearning.Infrastructure.DependencyInjectionModules;

internal static class AuthenticationModuleRegistration
{
    public static IServiceCollection AddAuthenticationModule(this IServiceCollection services)
    {
        services.AddScoped<SessionRevoker>();
        services.AddScoped<SecurityAuditLogger>();
        services.AddScoped<ISecurityAuditWriter>(provider => provider.GetRequiredService<SecurityAuditLogger>());
        services.AddScoped<IIdentityAuthenticationGateway, IdentityAuthenticationGateway>();
        services.AddScoped<JwtAccessTokenGenerator>();
        services.AddScoped<IAuthSessionIssuer, AuthSessionIssuer>();
        services.AddScoped<IRefreshSessionRotator, RefreshSessionRotator>();
        services.AddScoped<ISessionLogoutService, SessionLogoutService>();
        services.AddScoped<ILoginCommandHandler, LoginCommandHandler>();
        services.AddScoped<IGetCurrentUserQueryHandler, GetCurrentUserQueryHandler>();
        services.AddScoped<IRefreshSessionCommandHandler, RefreshSessionCommandHandler>();
        services.AddScoped<ILogoutCommandHandler, LogoutCommandHandler>();
        services.AddHostedService<AuthTokenCleanupService>();
        return services;
    }
}
