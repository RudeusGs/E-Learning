using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elearning.IntegrationTests;

[Collection(PostgresIntegrationGroup.Name)]
[Trait("Category", "Security")]
public sealed class AuthorizationIntegrationTests(IntegrationTestFactory factory)
{
    [Fact]
    public async Task FallbackPolicyRejectsUnannotatedControllerEndpoint()
    {
        using var protectedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddControllers().AddApplicationPart(typeof(UnannotatedTestController).Assembly)));
        using var client = protectedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var response = await client.GetAsync("/integration/unannotated");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHENTICATED", await response.GetProblemCodeAsync());
    }

    [Fact]
    public async Task StudentACannotReadStudentBResourcesByValidNumericIds()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.StudentAEmail);
        login.EnsureSuccessStatusCode();

        using var course = await client.GetAsync($"/api/student/courses/{factory.Seed.CourseBId}");
        using var lesson = await client.GetAsync($"/api/student/lessons/{factory.Seed.LessonB1Id}");
        using var answer = await client.PostJsonAsync(
            $"/api/student/questions/{factory.Seed.QuestionBId}/answer",
            new { optionId = factory.Seed.QuestionBCorrectOptionId });
        using var progress = await client.GetAsync($"/api/student/courses/{factory.Seed.CourseBId}/progress");

        Assert.Equal(HttpStatusCode.NotFound, course.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, lesson.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, answer.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, progress.StatusCode);
    }

    [Fact]
    public async Task LockedLessonReturnsStable403FromDirectApiCall()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient();
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();

        using var response = await client.GetAsync($"/api/student/lessons/{scenario.LessonIds[1]}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("LESSON_LOCKED", await response.GetProblemCodeAsync());
    }

    [Fact]
    public async Task StudentLessonNeverLeaksCorrectOptionBeforeSubmission()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.StudentAEmail);
        login.EnsureSuccessStatusCode();

        using var response = await client.GetAsync($"/api/student/lessons/{factory.Seed.LessonA1Id}");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("isCorrect", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("correctOption", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthenticatedBearerMutationDoesNotRequireCsrfToken()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient();
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();

        using var response = await client.PostJsonAsync(
            $"/api/student/lessons/{scenario.LessonIds[0]}/start",
            new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompletingLessonUnlocksNextAndUpdatesDerivedProgress()
    {
        var scenario = await factory.CreateScenarioAsync(5);
        using var client = CreateClient();
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();

        using var startForCompletion = await client.PostJsonAsync(
            $"/api/student/lessons/{scenario.LessonIds[0]}/start",
            new { });
        startForCompletion.EnsureSuccessStatusCode();
        using var passForCompletion = await client.PostJsonAsync(
            $"/api/student/questions/{scenario.QuestionId}/answer",
            new { optionId = scenario.CorrectOptionId });
        passForCompletion.EnsureSuccessStatusCode();

        using var complete = await client.PostJsonAsync(
            $"/api/student/lessons/{scenario.LessonIds[0]}/complete",
            new { });
        using var nextLesson = await client.GetAsync($"/api/student/lessons/{scenario.LessonIds[1]}");
        using var progressResponse = await client.GetAsync($"/api/student/courses/{scenario.CourseId}/progress");

        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, nextLesson.StatusCode);
        using var progress = JsonDocument.Parse(await progressResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, progress.RootElement.GetProperty("completedLessons").GetInt32());
        Assert.Equal(5, progress.RootElement.GetProperty("totalLessons").GetInt32());
        Assert.Equal(20, progress.RootElement.GetProperty("percentage").GetInt32());
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });
}