using System.Net;
using System.Text.Json;
using Elearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Elearning.IntegrationTests;

[Collection(PostgresIntegrationGroup.Name)]
public sealed class BusinessFeatureIntegrationTests(IntegrationTestFactory factory)
{
    [Fact]
    public async Task DashboardReflectsNewCourseStudentLessonsAndCompletion()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.AdminEmail);
        login.EnsureSuccessStatusCode();
        using var beforeResponse = await client.GetAsync("/api/admin/dashboard");
        beforeResponse.EnsureSuccessStatusCode();
        using var before = JsonDocument.Parse(await beforeResponse.Content.ReadAsStringAsync());
        var scenario = await factory.CreateScenarioAsync(2);

        using var studentClient = CreateClient();
        using var studentLogin = await studentClient.LoginAsync(scenario.StudentEmail);
        studentLogin.EnsureSuccessStatusCode();
        using var startForCompletion = await studentClient.PostJsonAsync(
            $"/api/student/lessons/{scenario.LessonIds[0]}/start",
            new { });
        startForCompletion.EnsureSuccessStatusCode();
        using var passForCompletion = await studentClient.PostJsonAsync(
            $"/api/student/questions/{scenario.QuestionId}/answer",
            new { optionId = scenario.CorrectOptionId });
        passForCompletion.EnsureSuccessStatusCode();
        using var complete = await studentClient.PostJsonAsync(
            $"/api/student/lessons/{scenario.LessonIds[0]}/complete",
            new { });
        complete.EnsureSuccessStatusCode();

        using var afterResponse = await client.GetAsync("/api/admin/dashboard");
        afterResponse.EnsureSuccessStatusCode();
        using var after = JsonDocument.Parse(await afterResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            before.RootElement.GetProperty("courseCount").GetInt32() + 1,
            after.RootElement.GetProperty("courseCount").GetInt32());
        Assert.Equal(
            before.RootElement.GetProperty("studentCount").GetInt32() + 1,
            after.RootElement.GetProperty("studentCount").GetInt32());
        Assert.Equal(
            before.RootElement.GetProperty("lessonCount").GetInt32() + 2,
            after.RootElement.GetProperty("lessonCount").GetInt32());
        Assert.Equal(
            before.RootElement.GetProperty("lessonCompletionCount").GetInt32() + 1,
            after.RootElement.GetProperty("lessonCompletionCount").GetInt32());
    }

    [Fact]
    public async Task AdminCanCreateUpdateEnrollAndDisableStudent()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.AdminEmail);
        login.EnsureSuccessStatusCode();
        var email = $"student-management-{Guid.NewGuid():N}@example.test";

        using var create = await client.PostJsonAsync("/api/admin/students", new
        {
            fullName = "Initial Student",
            email,
            initialPassword = IntegrationTestFactory.Password
        });
        create.EnsureSuccessStatusCode();
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var studentId = created.RootElement.GetProperty("id").GetInt64();

        using var update = await client.SendJsonAsync(
            HttpMethod.Put,
            $"/api/admin/students/{studentId}",
            new { fullName = "Updated Student" });
        update.EnsureSuccessStatusCode();

        var enrollmentRequest = new { studentId, courseId = factory.Seed.CourseAId };
        using var firstEnrollment = await client.PostJsonAsync("/api/admin/enrollments", enrollmentRequest);
        using var repeatedEnrollment = await client.PostJsonAsync("/api/admin/enrollments", enrollmentRequest);
        firstEnrollment.EnsureSuccessStatusCode();
        repeatedEnrollment.EnsureSuccessStatusCode();

        using var detail = await client.GetAsync($"/api/admin/students/{studentId}");
        detail.EnsureSuccessStatusCode();
        using var detailJson = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal("Updated Student", detailJson.RootElement.GetProperty("fullName").GetString());
        Assert.Single(detailJson.RootElement.GetProperty("enrollments").EnumerateArray());

        using var disable = await client.PostJsonAsync($"/api/admin/students/{studentId}/disable", new { });
        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);
        using var disabledDetail = await client.GetAsync($"/api/admin/students/{studentId}");
        using var disabledJson = JsonDocument.Parse(await disabledDetail.Content.ReadAsStringAsync());
        Assert.Equal("DISABLED", disabledJson.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task AdminCanManageLessonAndQuestionLifecycle()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.AdminEmail);
        login.EnsureSuccessStatusCode();

        using var createLesson = await client.PostJsonAsync(
            $"/api/admin/courses/{factory.Seed.CourseAId}/lessons",
            new
            {
                title = $"Managed lesson {Guid.NewGuid():N}",
                contentHtml = "<p>Initial</p>",
                sortOrder = 50,
                status = "DRAFT"
            });
        createLesson.EnsureSuccessStatusCode();
        using var lessonJson = JsonDocument.Parse(await createLesson.Content.ReadAsStringAsync());
        var lessonId = lessonJson.RootElement.GetProperty("id").GetInt64();
        var lessonVersion = lessonJson.RootElement.GetProperty("version").GetInt64();

        using var updateLesson = await client.SendJsonAsync(
            HttpMethod.Put,
            $"/api/admin/lessons/{lessonId}",
            new
            {
                title = "Managed lesson updated",
                contentHtml = "<p>Updated</p>",
                sortOrder = 50,
                status = "PUBLISHED",
                version = lessonVersion
            });
        updateLesson.EnsureSuccessStatusCode();

        using var createQuestion = await client.PostJsonAsync(
            $"/api/admin/lessons/{lessonId}/questions",
            new
            {
                text = "Original question",
                type = "TRUE_FALSE",
                explanation = "Original explanation",
                sortOrder = 1,
                options = new[]
                {
                    new { content = "Đúng", isCorrect = true, sortOrder = 1 },
                    new { content = "Sai", isCorrect = false, sortOrder = 2 }
                }
            });
        createQuestion.EnsureSuccessStatusCode();
        using var questionJson = JsonDocument.Parse(await createQuestion.Content.ReadAsStringAsync());
        var questionId = questionJson.RootElement.GetProperty("id").GetInt64();
        var questionVersion = questionJson.RootElement.GetProperty("version").GetInt64();

        using var updateQuestion = await client.SendJsonAsync(
            HttpMethod.Put,
            $"/api/admin/questions/{questionId}",
            new
            {
                text = "Updated question",
                type = "TRUE_FALSE",
                explanation = "Updated explanation",
                sortOrder = 1,
                version = questionVersion,
                options = new[]
                {
                    new { content = "Đúng", isCorrect = false, sortOrder = 1 },
                    new { content = "Sai", isCorrect = true, sortOrder = 2 }
                }
            });
        updateQuestion.EnsureSuccessStatusCode();

        using var list = await client.GetAsync($"/api/admin/lessons/{lessonId}/questions");
        list.EnsureSuccessStatusCode();
        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Equal("Updated question", listJson.RootElement[0].GetProperty("text").GetString());

        using var deleteQuestion = await client.DeleteAsync($"/api/admin/questions/{questionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteQuestion.StatusCode);
        using var archiveLesson = await client.DeleteAsync($"/api/admin/lessons/{lessonId}");
        Assert.Equal(HttpStatusCode.NoContent, archiveLesson.StatusCode);
        using var archivedLesson = await client.GetAsync($"/api/admin/lessons/{lessonId}");
        using var archivedJson = JsonDocument.Parse(await archivedLesson.Content.ReadAsStringAsync());
        Assert.Equal("ARCHIVED", archivedJson.RootElement.GetProperty("status").GetString());
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });
}
