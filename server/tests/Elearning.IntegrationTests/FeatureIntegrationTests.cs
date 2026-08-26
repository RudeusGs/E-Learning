using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Elearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elearning.IntegrationTests;

[Collection(PostgresIntegrationGroup.Name)]
public sealed class FeatureIntegrationTests(IntegrationTestFactory factory)
{
    [Fact]
    public async Task AdminCourseUpdateRejectsStaleVersion()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.AdminEmail);
        login.EnsureSuccessStatusCode();
        using var create = await client.PostJsonAsync("/api/admin/courses", new
        {
            title = $"Concurrency {Guid.NewGuid():N}",
            description = "Initial",
            status = "DRAFT",
            sortOrder = 100
        });
        create.EnsureSuccessStatusCode();
        using var createdJson = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = createdJson.RootElement.GetProperty("id").GetInt64();
        var version = createdJson.RootElement.GetProperty("version").GetInt64();
        var update = new
        {
            title = "Updated",
            description = "First writer",
            status = "DRAFT",
            sortOrder = 100,
            version
        };

        using var first = await client.SendJsonAsync(HttpMethod.Put, $"/api/admin/courses/{id}", update);
        using var stale = await client.SendJsonAsync(HttpMethod.Put, $"/api/admin/courses/{id}", update);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", await stale.GetProblemCodeAsync());
    }

    [Fact]
    public async Task LessonCreationSanitizesHtmlAndNormalizesYouTubeVideo()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.AdminEmail);
        login.EnsureSuccessStatusCode();
        var courseId = await CreateCourseAsync(client);

        using var create = await client.PostJsonAsync($"/api/admin/courses/{courseId}/lessons", new
        {
            title = "Secure lesson",
            contentHtml = "<p onclick=\"alert(1)\">Safe</p><script>alert(1)</script><a href=\"javascript:alert(1)\">x</a>",
            videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            sortOrder = 1,
            status = "PUBLISHED"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var json = await create.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var contentHtml = document.RootElement.GetProperty("contentHtml").GetString()!;
        Assert.DoesNotContain("<script", contentHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", contentHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", contentHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("YOUTUBE", document.RootElement.GetProperty("video").GetProperty("provider").GetString());
        Assert.Equal("dQw4w9WgXcQ", document.RootElement.GetProperty("video").GetProperty("externalId").GetString());
    }

    [Fact]
    public async Task InvalidQuestionShapeIsRejectedByDomainRule()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.AdminEmail);
        login.EnsureSuccessStatusCode();

        using var response = await client.PostJsonAsync(
            $"/api/admin/lessons/{factory.Seed.LessonA1Id}/questions",
            new
            {
                text = "Invalid",
                type = "MULTIPLE_CHOICE",
                sortOrder = 50,
                options = new[]
                {
                    new { content = "A", isCorrect = true, sortOrder = 1 },
                    new { content = "B", isCorrect = true, sortOrder = 2 }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", await response.GetProblemCodeAsync());
    }

    [Fact]
    public async Task AnswerAttemptsStoreImmutableCorrectnessSnapshots()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient();
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();

        using var correct = await client.PostJsonAsync(
            $"/api/student/questions/{scenario.QuestionId}/answer",
            new { optionId = scenario.CorrectOptionId });
        using var incorrect = await client.PostJsonAsync(
            $"/api/student/questions/{scenario.QuestionId}/answer",
            new { optionId = scenario.IncorrectOptionId });

        using var correctJson = JsonDocument.Parse(await correct.Content.ReadAsStringAsync());
        using var incorrectJson = JsonDocument.Parse(await incorrect.Content.ReadAsStringAsync());
        Assert.True(correctJson.RootElement.GetProperty("correct").GetBoolean());
        Assert.False(incorrectJson.RootElement.GetProperty("correct").GetBoolean());
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        var snapshots = await dbContext.StudentAnswers
            .AsNoTracking()
            .Where(answer => answer.StudentId == scenario.StudentId && answer.QuestionId == scenario.QuestionId)
            .OrderBy(answer => answer.Id)
            .Select(answer => answer.IsCorrect)
            .ToListAsync();
        Assert.Equal([true, false], snapshots);
    }

    [Fact]
    public async Task StudentCreationIgnoresAttemptedRoleMassAssignment()
    {
        using var adminClient = CreateClient();
        using var adminLogin = await adminClient.LoginAsync(IntegrationTestFactory.AdminEmail);
        adminLogin.EnsureSuccessStatusCode();
        var email = $"mass-assignment-{Guid.NewGuid():N}@example.test";

        using var create = await adminClient.PostJsonAsync("/api/admin/students", new
        {
            fullName = "Mass Assignment Student",
            email,
            initialPassword = IntegrationTestFactory.Password,
            role = "ADMIN",
            passwordHash = "attacker-controlled"
        });
        create.EnsureSuccessStatusCode();
        using var studentClient = CreateClient();
        using var login = await studentClient.LoginAsync(email);
        using var me = await studentClient.GetAsync("/api/auth/me");
        using var meJson = JsonDocument.Parse(await me.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("STUDENT", meJson.RootElement.GetProperty("role").GetString());
    }

    private static async Task<long> CreateCourseAsync(HttpClient client)
    {
        using var response = await client.PostJsonAsync("/api/admin/courses", new
        {
            title = $"Security {Guid.NewGuid():N}",
            status = "PUBLISHED",
            sortOrder = 200
        });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetInt64();
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });
}
