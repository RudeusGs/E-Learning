using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elearning.Infrastructure.Identity;
using Elearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Elearning.IntegrationTests;

[Collection(PostgresIntegrationGroup.Name)]
[Trait("Category", "Security")]
public sealed class AuthIntegrationTests(IntegrationTestFactory factory)
{
    private const string RefreshCookieName = "__Secure-elearning-refresh";

    [Fact]
    public async Task AnonymousProtectedRequestReturnsProblemDetails401()
    {
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHENTICATED", await response.GetProblemCodeAsync());
    }

    [Theory]
    [InlineData(IntegrationTestFactory.AdminEmail, "ADMIN")]
    [InlineData(IntegrationTestFactory.StudentAEmail, "STUDENT")]
    public async Task ValidLoginReturnsAccessTokenAndAuthenticatesMe(string email, string expectedRole)
    {
        using var client = CreateClient(factory);

        using var login = await client.LoginAsync(email);
        login.EnsureSuccessStatusCode();
        var loginJson = await login.Content.ReadAsStringAsync();
        using var me = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.DoesNotContain("refreshToken", loginJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("accessToken", loginJson, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal(expectedRole, document.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task LoginSetsOnlyHardenedRefreshCookie()
    {
        using var client = CreateClient(factory);

        using var login = await client.LoginAsync(IntegrationTestFactory.StudentAEmail);
        login.EnsureSuccessStatusCode();

        Assert.True(login.Headers.TryGetValues("Set-Cookie", out var setCookies));
        var cookie = Assert.Single(setCookies, value =>
            value.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal));
        Assert.Contains("; secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("; httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("; samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("; path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("; expires=", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("; domain=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(IntegrationTestFactory.StudentAEmail, "wrong-password")]
    [InlineData("unknown.integration@example.test", IntegrationTestFactory.Password)]
    [InlineData(IntegrationTestFactory.DisabledStudentEmail, IntegrationTestFactory.Password)]
    public async Task InvalidUnknownAndDisabledCredentialsUseGenericFailure(string email, string password)
    {
        using var client = CreateClient(factory);

        using var response = await client.LoginAsync(email, password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", await response.GetProblemCodeAsync());
    }

    [Fact]
    public async Task AccountLocksAfterConfiguredFailedPasswordThreshold()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient(factory);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await client.LoginAsync(scenario.StudentEmail, "definitely-wrong-password");
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        using var locked = await client.LoginAsync(scenario.StudentEmail);
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", await locked.GetProblemCodeAsync());
    }

    [Fact]
    public void IdentityPasswordAndLockoutPoliciesAreExplicitlyHardened()
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.Equal(10, options.Password.RequiredLength);
        Assert.Equal(4, options.Password.RequiredUniqueChars);
        Assert.False(options.Password.RequireDigit);
        Assert.False(options.Password.RequireLowercase);
        Assert.False(options.Password.RequireUppercase);
        Assert.False(options.Password.RequireNonAlphanumeric);
        Assert.True(options.Lockout.AllowedForNewUsers);
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Lockout.DefaultLockoutTimeSpan);
    }

    [Fact]
    public async Task StudentCannotCallAdminEndpoint()
    {
        using var client = CreateClient(factory);
        using var login = await client.LoginAsync(IntegrationTestFactory.StudentAEmail);
        login.EnsureSuccessStatusCode();

        using var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("FORBIDDEN", await response.GetProblemCodeAsync());
    }

    [Fact]
    public async Task TamperedAccessTokenIsRejected()
    {
        using var client = CreateClient(factory);
        using var login = await client.LoginAsync(IntegrationTestFactory.StudentAEmail);
        login.EnsureSuccessStatusCode();
        var accessToken = await login.GetAccessTokenAsync();
        var tampered = accessToken[..^1] + (accessToken[^1] == 'A' ? 'B' : 'A');
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Wrong.Issuer", "Elearning.Client", "HS256")]
    [InlineData("Elearning.Api", "Wrong.Audience", "HS256")]
    [InlineData("Elearning.Api", "Elearning.Client", "HS384")]
    public async Task AccessTokenWithWrongIssuerAudienceOrAlgorithmIsRejected(
        string issuer,
        string audience,
        string algorithm)
    {
        var scenario = await factory.CreateScenarioAsync();
        var user = await GetUserAsync(scenario.StudentId);
        var accessToken = CreateAccessToken(
            user,
            issuer,
            audience,
            algorithm,
            DateTimeOffset.UtcNow.AddMinutes(5));
        using var client = CreateClient(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredAccessTokenIsRejectedButRefreshCookieCanRenewSession()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient(factory);
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();
        var user = await GetUserAsync(scenario.StudentId);
        var expiredAccessToken = CreateAccessToken(
            user,
            "Elearning.Api",
            "Elearning.Client",
            "HS256",
            DateTimeOffset.UtcNow.AddMinutes(-5));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredAccessToken);

        using var protectedResponse = await client.GetAsync("/api/auth/me");
        using var refreshResponse = await client.PostAsync("/api/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task ChangedSecurityStampRejectsAccessAndRefreshTokens()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient(factory);
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(scenario.StudentId.ToString(CultureInfo.InvariantCulture));
            Assert.NotNull(user);
            Assert.True((await users.UpdateSecurityStampAsync(user)).Succeeded);
        }

        using var accessResponse = await client.GetAsync("/api/auth/me");
        client.DefaultRequestHeaders.Authorization = null;
        using var refreshResponse = await client.PostAsync("/api/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, accessResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        Assert.Equal("SESSION_REVOKED", await refreshResponse.GetProblemCodeAsync());
    }

    [Fact]
    public async Task DirectlyBlacklistedAccessTokenIsRejected()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient(factory);
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();
        var accessToken = await login.GetAccessTokenAsync();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
            dbContext.BlacklistedTokens.Add(new BlacklistedToken
            {
                TokenId = jwt.Id,
                UserId = scenario.StudentId,
                ExpiresAtUtc = new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero),
                RevokedAtUtc = DateTimeOffset.UtcNow,
                Reason = "SECURITY_TEST"
            });
            await dbContext.SaveChangesAsync();
        }

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenIsHashedAtRest()
    {
        using var client = CreateClient(factory);
        using var login = await client.LoginAsync(IntegrationTestFactory.StudentAEmail);
        login.EnsureSuccessStatusCode();
        var rawRefreshToken = GetRefreshCookie(login);
        var refreshTokenBytes = Base64UrlEncoder.DecodeBytes(rawRefreshToken);
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        var stored = await dbContext.RefreshTokens
            .AsNoTracking()
            .OrderByDescending(token => token.CreatedAtUtc)
            .FirstAsync(token => token.UserId == factory.Seed.StudentAId);

        Assert.Equal(expectedHash, stored.TokenHash);
        Assert.DoesNotContain(rawRefreshToken, stored.TokenHash, StringComparison.Ordinal);
        Assert.Equal(64, stored.TokenHash.Length);
        Assert.Equal(32, refreshTokenBytes.Length);
    }

    [Fact]
    public async Task RefreshRotationPersistsSingleUseChainAndNewCookie()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient(factory);
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();
        var originalToken = GetRefreshCookie(login);
        var originalHash = HashRefreshToken(originalToken);

        using var refresh = await client.PostAsync("/api/auth/refresh", content: null);
        refresh.EnsureSuccessStatusCode();
        var replacementToken = GetRefreshCookie(refresh);
        var replacementHash = HashRefreshToken(replacementToken);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        var original = await dbContext.RefreshTokens.AsNoTracking()
            .SingleAsync(token => token.TokenHash == originalHash);
        var replacement = await dbContext.RefreshTokens.AsNoTracking()
            .SingleAsync(token => token.TokenHash == replacementHash);

        Assert.NotEqual(originalToken, replacementToken);
        Assert.NotNull(original.UsedAtUtc);
        Assert.Equal(replacement.Id, original.ReplacedByTokenId);
        Assert.Equal(original.FamilyId, replacement.FamilyId);
        Assert.Equal(original.ExpiresAtUtc, replacement.ExpiresAtUtc);
        Assert.Null(replacement.UsedAtUtc);
        Assert.Null(replacement.RevokedAtUtc);
    }

    [Fact]
    public async Task ConcurrentRefreshConsumesTokenExactlyOnce()
    {
        using var loginClient = CreateClient(factory);
        using var login = await loginClient.LoginAsync(IntegrationTestFactory.StudentAEmail);
        login.EnsureSuccessStatusCode();
        var refreshToken = GetRefreshCookie(login);

        using var firstClient = CreateClient(factory, handleCookies: false);
        using var secondClient = CreateClient(factory, handleCookies: false);
        var responses = await Task.WhenAll(
            SendRefreshWithCookieAsync(firstClient, refreshToken),
            SendRefreshWithCookieAsync(secondClient, refreshToken));

        try
        {
            Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal("REFRESH_RETRY_REQUIRED", await conflict.GetProblemCodeAsync());
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task ConcurrentLogoutAndRefreshCannotLeaveReplacementSessionAlive()
    {
        var scenario = await factory.CreateScenarioAsync();

        for (var iteration = 0; iteration < 5; iteration++)
        {
            using var loginClient = CreateClient(factory, handleCookies: false);
            using var login = await loginClient.LoginAsync(scenario.StudentEmail);
            login.EnsureSuccessStatusCode();
            var refreshToken = GetRefreshCookie(login);

            using var refreshClient = CreateClient(factory, handleCookies: false);
            using var logoutClient = CreateClient(factory, handleCookies: false);
            var refreshTask = SendRefreshWithCookieAsync(refreshClient, refreshToken);
            var logoutTask = SendLogoutWithCookieAsync(logoutClient, refreshToken);
            await Task.WhenAll(refreshTask, logoutTask);
            using var refresh = await refreshTask;
            using var logout = await logoutTask;

            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            Assert.Contains(
                refresh.StatusCode,
                new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized });

            if (refresh.StatusCode == HttpStatusCode.OK)
            {
                var replacementToken = GetRefreshCookie(refresh);
                using var verificationClient = CreateClient(factory, handleCookies: false);
                using var verification = await SendRefreshWithCookieAsync(verificationClient, replacementToken);
                Assert.Equal(HttpStatusCode.Unauthorized, verification.StatusCode);
            }
        }
    }

    [Fact]
    public async Task ExpiredRefreshTokenIsRejected()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var loginClient = CreateClient(factory);
        using var login = await loginClient.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();
        var refreshToken = GetRefreshCookie(login);
        var tokenHash = HashRefreshToken(refreshToken);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
            await dbContext.RefreshTokens
                .Where(token => token.TokenHash == tokenHash)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(token => token.ExpiresAtUtc, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        using var refreshClient = CreateClient(factory, handleCookies: false);
        using var response = await SendRefreshWithCookieAsync(refreshClient, refreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("REFRESH_TOKEN_EXPIRED", await response.GetProblemCodeAsync());
    }

    [Fact]
    public async Task ExplicitlyRevokedRefreshTokenCannotBeUsed()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient(factory);
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();
        var refreshToken = GetRefreshCookie(login);

        using var logout = await client.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        using var replayClient = CreateClient(factory, handleCookies: false);
        using var response = await SendRefreshWithCookieAsync(replayClient, refreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", await response.GetProblemCodeAsync());
    }

    [Fact]
    public async Task ReusedRefreshTokenRevokesEntireFamily()
    {
        using var client = CreateClient(factory);
        using var login = await client.LoginAsync(IntegrationTestFactory.StudentAEmail);
        login.EnsureSuccessStatusCode();
        var oldRefreshToken = GetRefreshCookie(login);

        using var rotated = await client.PostAsync("/api/auth/refresh", content: null);
        rotated.EnsureSuccessStatusCode();
        var rotatedAccessToken = await rotated.GetAccessTokenAsync();

        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        using var replayClient = CreateClient(factory, handleCookies: false);
        using var replay = await SendRefreshWithCookieAsync(replayClient, oldRefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal("REFRESH_TOKEN_REUSE_DETECTED", await replay.GetProblemCodeAsync());

        client.DefaultRequestHeaders.Authorization = null;
        using var familyRefresh = await client.PostAsync("/api/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, familyRefresh.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rotatedAccessToken);
        using var revokedAccess = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revokedAccess.StatusCode);
    }

    [Fact]
    public async Task LogoutRevokesOnlyThePresentedSessionFamily()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var firstSession = CreateClient(factory);
        using var secondSession = CreateClient(factory);
        using var firstLogin = await firstSession.LoginAsync(scenario.StudentEmail);
        using var secondLogin = await secondSession.LoginAsync(scenario.StudentEmail);
        firstLogin.EnsureSuccessStatusCode();
        secondLogin.EnsureSuccessStatusCode();

        using var logout = await firstSession.PostAsync("/api/auth/logout", content: null);
        using var secondMe = await secondSession.GetAsync("/api/auth/me");
        secondSession.DefaultRequestHeaders.Authorization = null;
        using var secondRefresh = await secondSession.PostAsync("/api/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondMe.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondRefresh.StatusCode);
    }

    [Fact]
    public async Task LogoutRevokesAccessAndRefreshSession()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient(factory);
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();

        using var logout = await client.PostAsync("/api/auth/logout", content: null);
        using var me = await client.GetAsync("/api/auth/me");
        client.DefaultRequestHeaders.Authorization = null;
        using var refresh = await client.PostAsync("/api/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task DisablingStudentImmediatelyRevokesExistingSession()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var studentClient = CreateClient(factory);
        using var studentLogin = await studentClient.LoginAsync(scenario.StudentEmail);
        studentLogin.EnsureSuccessStatusCode();

        using var adminClient = CreateClient(factory);
        using var adminLogin = await adminClient.LoginAsync(IntegrationTestFactory.AdminEmail);
        adminLogin.EnsureSuccessStatusCode();
        using var disable = await adminClient.PostJsonAsync(
            $"/api/admin/students/{scenario.StudentId}/disable",
            new { });
        disable.EnsureSuccessStatusCode();

        using var me = await studentClient.GetAsync("/api/auth/me");
        studentClient.DefaultRequestHeaders.Authorization = null;
        using var refresh = await studentClient.PostAsync("/api/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task AuthenticationEndpointReturns429WhenLimitIsExceeded()
    {
        using var limitedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:AuthenticationPermitLimit"] = "2"
                })));
        using var client = CreateClient(limitedFactory);

        using var first = await client.LoginAsync("rate-limit-1@example.test", "invalid-password");
        using var second = await client.LoginAsync("rate-limit-2@example.test", "invalid-password");
        using var third = await client.LoginAsync("rate-limit-3@example.test", "invalid-password");

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Equal("RATE_LIMITED", await third.GetProblemCodeAsync());
        Assert.True(third.Headers.RetryAfter is not null);
    }

    private async Task<ApplicationUser> GetUserAsync(long userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        return await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == userId);
    }

    private static string CreateAccessToken(
        ApplicationUser user,
        string issuer,
        string audience,
        string algorithm,
        DateTimeOffset expiresAtUtc)
    {
        var now = DateTimeOffset.UtcNow;
        var notBefore = expiresAtUtc > now ? now.AddMinutes(-1) : expiresAtUtc.AddMinutes(-5);
        var securityStampHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(user.SecurityStamp ?? string.Empty)));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("session_stamp", securityStampHash),
            new Claim(ClaimTypes.Role, "Student")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(IntegrationTestFactory.JwtSigningKey));
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore.UtcDateTime,
            expiresAtUtc.UtcDateTime,
            new SigningCredentials(key, algorithm));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private static HttpClient CreateClient(
        WebApplicationFactory<Program> application,
        bool handleCookies = true) =>
        application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = handleCookies
        });

    private static string GetRefreshCookie(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookies));
        var cookie = setCookies.Single(value =>
            value.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal));
        var pair = cookie.Split(';', 2)[0];
        return pair[(pair.IndexOf('=') + 1)..];
    }

    private static async Task<HttpResponseMessage> SendRefreshWithCookieAsync(
        HttpClient client,
        string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"{RefreshCookieName}={refreshToken}");
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendLogoutWithCookieAsync(
        HttpClient client,
        string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("Cookie", $"{RefreshCookieName}={refreshToken}");
        return await client.SendAsync(request);
    }
}
