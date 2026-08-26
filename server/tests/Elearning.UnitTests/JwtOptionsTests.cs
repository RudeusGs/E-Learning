using Elearning.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Elearning.UnitTests;

[Trait("Category", "Security")]
public sealed class JwtOptionsTests
{
    [Fact]
    public void MissingSigningKeyFailsConfiguration()
    {
        var configuration = CreateConfiguration();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            JwtOptions.FromConfiguration(configuration, new TestHostEnvironment(Environments.Development)));

        Assert.Contains("Jwt:Key is required", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShortSigningKeyFailsConfiguration()
    {
        var configuration = CreateConfiguration(("Jwt:Key", "too-short"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            JwtOptions.FromConfiguration(configuration, new TestHostEnvironment(Environments.Development)));

        Assert.Contains("at least 32 UTF-8 bytes", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionPlaceholderSigningKeyFailsConfiguration()
    {
        var configuration = CreateConfiguration(
            ("Jwt:Key", "change-me-example-placeholder-key-material-123456789"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            JwtOptions.FromConfiguration(configuration, new TestHostEnvironment(Environments.Production)));

        Assert.Contains("production secret", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidConfigurationUsesBoundedLifetimes()
    {
        var configuration = CreateConfiguration(
            ("Jwt:Issuer", "Test.Issuer"),
            ("Jwt:Audience", "Test.Audience"),
            ("Jwt:Key", "a-secure-test-key-material-with-at-least-thirty-two-bytes"),
            ("Jwt:AccessTokenMinutes", "15"),
            ("Jwt:RefreshTokenDays", "20"),
            ("Jwt:ClockSkewSeconds", "10"),
            ("Jwt:ConcurrentRefreshGraceSeconds", "3"));

        var options = JwtOptions.FromConfiguration(
            configuration,
            new TestHostEnvironment(Environments.Development));

        Assert.Equal("Test.Issuer", options.Issuer);
        Assert.Equal("Test.Audience", options.Audience);
        Assert.Equal(TimeSpan.FromMinutes(15), options.AccessTokenLifetime);
        Assert.Equal(TimeSpan.FromDays(20), options.RefreshTokenLifetime);
        Assert.Equal(TimeSpan.FromSeconds(10), options.ClockSkew);
        Assert.Equal(TimeSpan.FromSeconds(3), options.ConcurrentRefreshGrace);
    }

    private static IConfiguration CreateConfiguration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(item => item.Key, item => item.Value))
            .Build();

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Elearning.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
