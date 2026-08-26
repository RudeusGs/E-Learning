using System.Net;
using System.Text.Json;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elearning.IntegrationTests;

[Collection(PostgresIntegrationGroup.Name)]
public sealed class PersistenceIntegrationTests(IntegrationTestFactory factory)
{
    [Fact]
    public async Task CleanDatabaseHasEverySourceControlledMigrationApplied()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();

        var available = dbContext.Database.GetMigrations().ToList();
        var applied = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();

        Assert.NotEmpty(available);
        Assert.Equal(available, applied);
        Assert.True(await dbContext.Database.CanConnectAsync());
    }

    [Fact]
    public async Task ConcurrentEnrollmentRequestsCreateOneRow()
    {
        var scenario = await factory.CreateScenarioAsync(enroll: false);
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.AdminEmail);
        login.EnsureSuccessStatusCode();
        var body = new { studentId = scenario.StudentId, courseId = scenario.CourseId };

        var responses = await Task.WhenAll(
            client.SendJsonAsync(HttpMethod.Post, "/api/admin/enrollments", body),
            client.SendJsonAsync(HttpMethod.Post, "/api/admin/enrollments", body));

        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        Assert.Equal(1, await dbContext.Enrollments.CountAsync(enrollment =>
            enrollment.StudentId == scenario.StudentId && enrollment.CourseId == scenario.CourseId));
    }

    [Fact]
    public async Task ConcurrentLessonStartCreatesOneStableProgressRow()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient();
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();
        var path = $"/api/student/lessons/{scenario.LessonIds[0]}/start";

        var responses = await Task.WhenAll(
            client.SendJsonAsync(HttpMethod.Post, path, new { }),
            client.SendJsonAsync(HttpMethod.Post, path, new { }));
        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        Assert.Equal(1, await dbContext.LessonProgress.CountAsync(progress =>
            progress.StudentId == scenario.StudentId && progress.LessonId == scenario.LessonIds[0]));
    }

    [Fact]
    public async Task ConcurrentCompletionPreservesFirstCompletionTimestamp()
    {
        var scenario = await factory.CreateScenarioAsync();
        using var client = CreateClient();
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();
        using var start = await client.PostJsonAsync(
            $"/api/student/lessons/{scenario.LessonIds[0]}/start",
            new { });
        start.EnsureSuccessStatusCode();
        var path = $"/api/student/lessons/{scenario.LessonIds[0]}/complete";

        var responses = await Task.WhenAll(
            client.SendJsonAsync(HttpMethod.Post, path, new { }),
            client.SendJsonAsync(HttpMethod.Post, path, new { }));
        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
            var timestamps = await Task.WhenAll(responses.Select(async response =>
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return json.RootElement.GetProperty("completedAtUtc").GetDateTimeOffset();
            }));
            Assert.Equal(timestamps[0], timestamps[1]);
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
    public async Task DatabaseRejectsOptionFromAnotherQuestion()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        dbContext.StudentAnswers.Add(StudentAnswer.Create(
            factory.Seed.StudentAId,
            factory.Seed.QuestionAId,
            factory.Seed.QuestionBCorrectOptionId,
            true,
            DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task CourseCursorQueryHasNoOffsetAndDoesNotRepeatRows()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.AdminEmail);
        login.EnsureSuccessStatusCode();
        factory.Commands.Clear();

        using var first = await client.GetAsync("/api/admin/courses?limit=1");
        first.EnsureSuccessStatusCode();
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var firstId = firstJson.RootElement.GetProperty("items")[0].GetProperty("id").GetInt64();
        var cursor = Uri.EscapeDataString(firstJson.RootElement.GetProperty("nextCursor").GetString()!);
        var firstCommands = factory.Commands.Commands.ToList();
        factory.Commands.Clear();

        using var second = await client.GetAsync($"/api/admin/courses?limit=1&cursor={cursor}");
        second.EnsureSuccessStatusCode();
        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        var secondId = secondJson.RootElement.GetProperty("items")[0].GetProperty("id").GetInt64();
        var secondCommands = factory.Commands.Commands.ToList();

        Assert.NotEqual(firstId, secondId);
        Assert.All(firstCommands.Concat(secondCommands), command =>
            Assert.DoesNotContain("OFFSET", command, StringComparison.OrdinalIgnoreCase));
        Assert.InRange(firstCommands.Count, 1, 10);
        Assert.InRange(secondCommands.Count, 1, 10);
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });
}
