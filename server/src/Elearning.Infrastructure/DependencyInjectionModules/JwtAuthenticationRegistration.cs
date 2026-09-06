using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Elearning.Application.Auth.Security;
using Elearning.Domain;
using Elearning.Infrastructure.Auth;
using Elearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Elearning.Infrastructure.DependencyInjectionModules;

internal static class JwtAuthenticationRegistration
{
    public static IServiceCollection AddJwtAuthenticationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwtOptions = JwtOptions.FromConfiguration(configuration, environment);
        services.AddSingleton(jwtOptions);
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = false;
                options.TokenValidationParameters = CreateValidationParameters(jwtOptions);
                options.Events = CreateEvents();
            });

        return services;
    }

    private static TokenValidationParameters CreateValidationParameters(JwtOptions options) => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidIssuer = options.Issuer,
        ValidAudience = options.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidTypes = [JwtTokenDefaults.TokenType],
        ClockSkew = options.ClockSkew,
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };

    private static JwtBearerEvents CreateEvents() => new()
    {
        OnTokenValidated = ValidateSessionAsync,
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    };

    private static async Task ValidateSessionAsync(TokenValidatedContext context)
    {
        var claims = ReadRequiredClaims(context.Principal);
        if (claims is null)
        {
            context.Fail("Required authentication claims are missing.");
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ElearningDbContext>();
        var now = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow();

        var sessionState = await dbContext.Users
            .AsNoTracking()
            .Where(candidate => candidate.Id == claims.UserId)
            .Select(candidate => new
            {
                candidate.Status,
                candidate.SecurityStamp,
                IsBlacklisted = dbContext.BlacklistedTokens.Any(token =>
                    token.TokenId == claims.Jti &&
                    token.UserId == claims.UserId &&
                    token.ExpiresAtUtc > now)
            })
            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

        if (sessionState is null || sessionState.Status != AccountStatus.Active)
        {
            context.Fail("Account session is no longer valid.");
            return;
        }

        if (!string.Equals(TokenSecurity.HashSecurityStamp(sessionState.SecurityStamp), claims.SessionStamp, StringComparison.Ordinal))
        {
            context.Fail("Account session is no longer valid.");
            return;
        }

        if (sessionState.IsBlacklisted)
        {
            context.Fail("Token has been revoked.");
        }
    }

    private static RequiredClaims? ReadRequiredClaims(ClaimsPrincipal? principal)
    {
        var userIdValue = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var sessionStamp = principal?.FindFirst(AuthClaimNames.SessionStamp)?.Value;
        var jti = principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        return long.TryParse(userIdValue, out var userId) &&
               !string.IsNullOrWhiteSpace(sessionStamp) &&
               !string.IsNullOrWhiteSpace(jti)
            ? new RequiredClaims(userId, sessionStamp, jti)
            : null;
    }

    private sealed record RequiredClaims(long UserId, string SessionStamp, string Jti);
}
