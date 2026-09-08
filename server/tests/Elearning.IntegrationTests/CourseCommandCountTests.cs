using System.Net.Http.Json;
using System.Text.Json;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elearning.IntegrationTests;

[Collection(PostgresIntegrationGroup.Name)]
public sealed class CourseCommandCountTests(IntegrationTestFactory factory)
{
    [Fact]
    public async Task AdminCourseListCommandCountIsBounded()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.AdminEmail);
        login.EnsureSuccessStatusCode();

        await CreateDatasetAsync(5);
        factory.Commands.Clear();
        using var smallResponse = await client.GetAsync("/api/admin/courses?limit=5");
        smallResponse.EnsureSuccessStatusCode();
        var smallCount = factory.Commands.Commands.Count;

        await CreateDatasetAsync(20);
        factory.Commands.Clear();
        using var largeResponse = await client.GetAsync("/api/admin/courses?limit=25");
        largeResponse.EnsureSuccessStatusCode();
        var largeCount = factory.Commands.Commands.Count;

        // Expect 2 logical commands (Session Auth + Projection), which appear as 4 due to Interceptor/DI duplicate registrations.
        Assert.True(smallCount <= 4, $"Expected <= 4 commands but got {smallCount}:\n{string.Join("\n---\n", factory.Commands.Commands)}");
        Assert.True(largeCount <= 4, $"Expected <= 4 commands but got {largeCount}");
        Assert.Equal(smallCount, largeCount);
    }

    [Fact]
    public async Task StudentCourseListCommandCountIsBounded()
    {
        var scenario = await factory.CreateScenarioAsync(lessonCount: 5, enroll: true);
        using var client = CreateClient();
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();

        factory.Commands.Clear();
        using var smallResponse = await client.GetAsync("/api/student/courses?limit=5");
        smallResponse.EnsureSuccessStatusCode();
        var smallCount = factory.Commands.Commands.Count;

        for (int i = 0; i < 20; i++)
        {
            await factory.CreateScenarioAsync(lessonCount: 5, enroll: false);
            // the student is not enrolled, so we'll enroll them explicitly via db
        }

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        var newCourses = await db.Courses.OrderByDescending(c => c.Id).Take(20).ToListAsync();
        foreach (var c in newCourses)
        {
            db.Enrollments.Add(Enrollment.Create(scenario.StudentId, c.Id, DateTimeOffset.UtcNow));
        }
        await db.SaveChangesAsync();

        factory.Commands.Clear();
        using var largeResponse = await client.GetAsync("/api/student/courses?limit=25");
        largeResponse.EnsureSuccessStatusCode();
        var largeCount = factory.Commands.Commands.Count;

        // Expect 2 logical commands (Session Auth + Projection), which appear as 4 due to Interceptor/DI duplicate registrations.
        Assert.True(smallCount <= 4, $"Expected <= 4 commands but got {smallCount}");
        Assert.True(largeCount <= 4, $"Expected <= 4 commands but got {largeCount}");
        Assert.Equal(smallCount, largeCount);
    }

    [Fact]
    public async Task StudentCourseDetailCommandCountIsBounded()
    {
        var smallScenario = await factory.CreateScenarioAsync(lessonCount: 5, enroll: true);
        using var client = CreateClient();
        using var login = await client.LoginAsync(smallScenario.StudentEmail);
        login.EnsureSuccessStatusCode();

        factory.Commands.Clear();
        using var smallResponse = await client.GetAsync($"/api/student/courses/{smallScenario.CourseId}");
        smallResponse.EnsureSuccessStatusCode();
        var smallCount = factory.Commands.Commands.Count;

        var largeScenario = await factory.CreateScenarioAsync(lessonCount: 50, enroll: true);
        using var login2 = await client.LoginAsync(largeScenario.StudentEmail);
        login2.EnsureSuccessStatusCode();

        factory.Commands.Clear();
        using var largeResponse = await client.GetAsync($"/api/student/courses/{largeScenario.CourseId}");
        largeResponse.EnsureSuccessStatusCode();
        var largeCount = factory.Commands.Commands.Count;

        // Expect 2 logical commands (Session Auth + Projection), which appear as 4 due to Interceptor/DI duplicate registrations.
        Assert.True(smallCount <= 4, $"Expected <= 4 commands but got {smallCount}");
        Assert.True(largeCount <= 4, $"Expected <= 4 commands but got {largeCount}");
        Assert.Equal(smallCount, largeCount);
    }

    [Fact]
    public async Task StudentProgressCommandCountDoesNotGrowWithLessonCount()
    {
        var smallScenario = await factory.CreateScenarioAsync(lessonCount: 5, enroll: true);
        using var client = CreateClient();
        using var login = await client.LoginAsync(smallScenario.StudentEmail);
        login.EnsureSuccessStatusCode();

        factory.Commands.Clear();
        using var smallResponse = await client.GetAsync($"/api/student/courses/{smallScenario.CourseId}/progress");
        smallResponse.EnsureSuccessStatusCode();
        var smallCount = factory.Commands.Commands.Count;

        var largeScenario = await factory.CreateScenarioAsync(lessonCount: 80, enroll: true);
        using var largeLogin = await client.LoginAsync(largeScenario.StudentEmail);
        largeLogin.EnsureSuccessStatusCode();

        factory.Commands.Clear();
        using var largeResponse = await client.GetAsync($"/api/student/courses/{largeScenario.CourseId}/progress");
        largeResponse.EnsureSuccessStatusCode();
        var largeCount = factory.Commands.Commands.Count;

        AssertCommandCountIsConstant(smallCount, largeCount, maximum: 6);
    }

    [Fact]
    public async Task StudentLessonCommandCountDoesNotGrowWithQuestionCount()
    {
        var scenario = await factory.CreateScenarioAsync(lessonCount: 2, enroll: true);
        using var client = CreateClient();
        using var login = await client.LoginAsync(scenario.StudentEmail);
        login.EnsureSuccessStatusCode();

        factory.Commands.Clear();
        using var smallResponse = await client.GetAsync($"/api/student/lessons/{scenario.LessonIds[0]}");
        smallResponse.EnsureSuccessStatusCode();
        var smallCount = factory.Commands.Commands.Count;

        await AddQuestionsAsync(scenario.LessonIds[0], 60);
        factory.Commands.Clear();
        using var largeResponse = await client.GetAsync($"/api/student/lessons/{scenario.LessonIds[0]}");
        largeResponse.EnsureSuccessStatusCode();
        var largeCount = factory.Commands.Commands.Count;

        Assert.True(smallCount <= 8 && largeCount <= 8 && smallCount == largeCount,
            $"Expected <= 8 commands. Small: {smallCount}, Large: {largeCount}\n\nLarge Commands:\n" + string.Join("\n---\n", factory.Commands.Commands));
    }

    [Fact]
    public async Task AdminProgressCommandCountDoesNotGrowWithEnrollmentCount()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.AdminEmail);
        login.EnsureSuccessStatusCode();

        factory.Commands.Clear();
        using var smallResponse = await client.GetAsync("/api/admin/progress?limit=5");
        smallResponse.EnsureSuccessStatusCode();
        var smallCount = factory.Commands.Commands.Count;

        var courseIds = await CreateDatasetAsync(30);
        await EnrollStudentAsync(factory.Seed.StudentAId, courseIds);
        factory.Commands.Clear();
        using var largeResponse = await client.GetAsync("/api/admin/progress?limit=25");
        largeResponse.EnsureSuccessStatusCode();
        var largeCount = factory.Commands.Commands.Count;

        AssertCommandCountIsConstant(smallCount, largeCount, maximum: 4);
    }

    [Fact]
    public async Task DashboardCommandCountDoesNotGrowWithDatasetSize()
    {
        using var client = CreateClient();
        using var login = await client.LoginAsync(IntegrationTestFactory.AdminEmail);
        login.EnsureSuccessStatusCode();

        factory.Commands.Clear();
        using var smallResponse = await client.GetAsync("/api/admin/dashboard");
        smallResponse.EnsureSuccessStatusCode();
        var smallCount = factory.Commands.Commands.Count;

        await CreateDatasetAsync(100);
        factory.Commands.Clear();
        using var largeResponse = await client.GetAsync("/api/admin/dashboard");
        largeResponse.EnsureSuccessStatusCode();
        var largeCount = factory.Commands.Commands.Count;

        AssertCommandCountIsConstant(smallCount, largeCount, maximum: 10);
    }

    private async Task<IReadOnlyList<long>> CreateDatasetAsync(int courseCount)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        var courses = new List<Course>(courseCount);
        for (int i = 0; i < courseCount; i++)
        {
            var course = Course.Create($"Integration Course {Guid.NewGuid():N}", null, null, i, CourseStatus.Published, DateTimeOffset.UtcNow);
            courses.Add(course);
        }
        db.Courses.AddRange(courses);
        await db.SaveChangesAsync();
        return courses.Select(course => course.Id).ToList();
    }

    private async Task EnrollStudentAsync(long studentId, IReadOnlyCollection<long> courseIds)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        db.Enrollments.AddRange(courseIds.Select(courseId =>
            Enrollment.Create(studentId, courseId, DateTimeOffset.UtcNow)));
        await db.SaveChangesAsync();
    }

    private async Task AddQuestionsAsync(long lessonId, int questionCount)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        var now = DateTimeOffset.UtcNow;
        var questions = Enumerable.Range(1, questionCount).Select(index =>
            Question.Create(
                lessonId,
                $"Load question {index}",
                QuestionType.MultipleChoice,
                "Explanation",
                index + 1,
                [
                    new QuestionOptionDraft("Correct", true, 1),
                    new QuestionOptionDraft("Incorrect", false, 2)
                ],
                now));
        db.Questions.AddRange(questions);
        await db.SaveChangesAsync();
    }

    private static void AssertCommandCountIsConstant(int smallCount, int largeCount, int maximum)
    {
        Assert.True(smallCount <= maximum, $"Expected <= {maximum} commands but got {smallCount}");
        Assert.True(largeCount <= maximum, $"Expected <= {maximum} commands but got {largeCount}");
        Assert.Equal(smallCount, largeCount);
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });
}
