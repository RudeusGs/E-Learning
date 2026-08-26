using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Elearning.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthReturnsOk()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Jwt:Key", "integration-only-jwt-signing-key-with-at-least-thirty-two-bytes-2026");
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
