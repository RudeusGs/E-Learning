using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Elearning.Infrastructure.Auth;

public sealed record JwtOptions(
    string Issuer,
    string Audience,
    string SigningKey,
    TimeSpan AccessTokenLifetime,
    TimeSpan RefreshTokenLifetime,
    TimeSpan ClockSkew,
    TimeSpan ConcurrentRefreshGrace)
{
    public static JwtOptions FromConfiguration(IConfiguration configuration, IHostEnvironment environment)
    {
        var issuer = GetValueOrDefault(configuration, JwtConfigurationKeys.Issuer, "Elearning.Api");
        var audience = GetValueOrDefault(configuration, JwtConfigurationKeys.Audience, "Elearning.Client");
        var signingKey = GetSigningKey(configuration, environment);
        var accessMinutes = GetBoundedValue(configuration, JwtConfigurationKeys.AccessTokenMinutes, 10, 1, 30);
        var refreshDays = GetBoundedValue(configuration, JwtConfigurationKeys.RefreshTokenDays, 30, 1, 90);
        var clockSkewSeconds = GetBoundedValue(configuration, JwtConfigurationKeys.ClockSkewSeconds, 30, 0, 120);
        var concurrentGraceSeconds = GetBoundedValue(
            configuration,
            JwtConfigurationKeys.ConcurrentRefreshGraceSeconds,
            5,
            1,
            30);

        return new JwtOptions(
            issuer,
            audience,
            signingKey,
            TimeSpan.FromMinutes(accessMinutes),
            TimeSpan.FromDays(refreshDays),
            TimeSpan.FromSeconds(clockSkewSeconds),
            TimeSpan.FromSeconds(concurrentGraceSeconds));
    }

    private static string GetValueOrDefault(
        IConfiguration configuration,
        string key,
        string defaultValue)
    {
        var value = configuration[key]?.Trim();
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static string GetSigningKey(IConfiguration configuration, IHostEnvironment environment)
    {
        var signingKey = configuration[JwtConfigurationKeys.SigningKey]?.Trim();
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "Jwt:Key is required. Configure it through a secret store or environment variable.");
        }

        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must contain at least 32 UTF-8 bytes of key material.");
        }

        if (environment.IsProduction() && IsKnownPlaceholder(signingKey))
        {
            throw new InvalidOperationException("Jwt:Key must be a production secret, not a sample or development value.");
        }

        return signingKey;
    }

    private static int GetBoundedValue(
        IConfiguration configuration,
        string key,
        int defaultValue,
        int minimum,
        int maximum)
    {
        var value = configuration.GetValue(key, defaultValue);
        if (value < minimum || value > maximum)
        {
            throw new InvalidOperationException($"{key} must be between {minimum} and {maximum}.");
        }

        return value;
    }

    private static bool IsKnownPlaceholder(string signingKey) =>
        signingKey.Contains("change-me", StringComparison.OrdinalIgnoreCase) ||
        signingKey.Contains("development", StringComparison.OrdinalIgnoreCase) ||
        signingKey.Contains("integration-only", StringComparison.OrdinalIgnoreCase) ||
        signingKey.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
        signingKey.Contains("example", StringComparison.OrdinalIgnoreCase);
}
