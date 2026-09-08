using Elearning.Domain;
using Elearning.Infrastructure.Identity;
using Elearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Elearning.IntegrationTests;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private sealed class DummyCacheService : Elearning.Application.Common.Interfaces.ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public const string AdminEmail = "admin.integration@example.test";
    public const string StudentAEmail = "student-a.integration@example.test";
    public const string StudentBEmail = "student-b.integration@example.test";
    public const string DisabledStudentEmail = "disabled.integration@example.test";
    public const string Password = "Correct-Horse-Battery";
    public const string JwtSigningKey = "integration-only-jwt-signing-key-with-at-least-thirty-two-bytes-2026";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("elearning_tests")
        .WithUsername("elearning_tests")
        .WithPassword("integration-only-password")
        .Build();

    public TestSeed Seed { get; private set; } = null!;
    public CommandCaptureInterceptor Commands { get; } = new();

    public async Task<TestScenario> CreateScenarioAsync(int lessonCount = 2, bool enroll = true)
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        var now = DateTimeOffset.UtcNow;
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"student-{suffix}@example.test";
        var student = await CreateUserAsync(users, email, $"Student {suffix}", UserRole.Student, now);
        var course = Course.Create($"Course {suffix}", null, null, 1, CourseStatus.Published, now);
        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        var lessons = Enumerable.Range(1, lessonCount)
            .Select(index => Lesson.Create(
                course.Id,
                $"Lesson {index}",
                null,
                $"<p>Lesson {index}</p>",
                null,
                null,
                index,
                LessonStatus.Published,
                now))
            .ToList();
        dbContext.Lessons.AddRange(lessons);
        await dbContext.SaveChangesAsync();

        var question = CreateQuestion(lessons[0].Id, $"Question {suffix}", now);
        dbContext.Questions.Add(question);
        if (enroll)
        {
            dbContext.Enrollments.Add(Enrollment.Create(student.Id, course.Id, now));
        }
        await dbContext.SaveChangesAsync();

        return new TestScenario(
            email,
            student.Id,
            course.Id,
            lessons.Select(lesson => lesson.Id).ToList(),
            question.Id,
            question.Options.Single(option => option.IsCorrect).Id,
            question.Options.Single(option => !option.IsCorrect).Id);
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElearningDbContext>();
        await dbContext.Database.MigrateAsync();
        Seed = await SeedAsync(scope.ServiceProvider, dbContext);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", JwtSigningKey);
        builder.UseSetting("Jwt:ConcurrentRefreshGraceSeconds", "1");
        builder.UseSetting("RateLimiting:AuthenticationPermitLimit", "100");
        builder.UseSetting("RateLimiting:RefreshPermitLimit", "100");
        builder.ConfigureServices(services =>
        {
            // Minimal-host configuration is consumed while Program registers the context.
            // Replace that registration explicitly so tests can never fall back to a
            // developer's local/Compose database when provider ordering changes.
            services.RemoveAll<DbContextOptions<ElearningDbContext>>();
            services.RemoveAll<ElearningDbContext>();
            services.RemoveAll<Elearning.Application.Common.Interfaces.ICacheService>();
            services.AddSingleton<IInterceptor>(Commands);
            services.AddSingleton<Elearning.Application.Common.Interfaces.ICacheService, DummyCacheService>();
            services.AddDbContext<ElearningDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(_postgres.GetConnectionString());
                options.AddInterceptors(serviceProvider.GetServices<IInterceptor>());
            });
        });
    }

    private static async Task<TestSeed> SeedAsync(IServiceProvider services, ElearningDbContext dbContext)
    {
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var now = new DateTimeOffset(2026, 8, 25, 0, 0, 0, TimeSpan.Zero);
        var admin = await CreateUserAsync(users, AdminEmail, "Integration Admin", UserRole.Admin, now);
        var studentA = await CreateUserAsync(users, StudentAEmail, "Student A", UserRole.Student, now);
        var studentB = await CreateUserAsync(users, StudentBEmail, "Student B", UserRole.Student, now);
        var disabled = await CreateUserAsync(users, DisabledStudentEmail, "Disabled Student", UserRole.Student, now);
        disabled.Disable(now.AddMinutes(1));
        Assert.True((await users.UpdateAsync(disabled)).Succeeded);
        Assert.True((await users.UpdateSecurityStampAsync(disabled)).Succeeded);

        var courseA = Course.Create("Course A", null, null, 1, CourseStatus.Published, now);
        var courseB = Course.Create("Course B", null, null, 2, CourseStatus.Published, now);
        dbContext.Courses.AddRange(courseA, courseB);
        await dbContext.SaveChangesAsync();

        var lessonA1 = Lesson.Create(courseA.Id, "A1", null, "<p>A1</p>", null, null, 1, LessonStatus.Published, now);
        var lessonA2 = Lesson.Create(courseA.Id, "A2", null, "<p>A2</p>", null, null, 2, LessonStatus.Published, now);
        var lessonB1 = Lesson.Create(courseB.Id, "B1", null, "<p>B1</p>", null, null, 1, LessonStatus.Published, now);
        dbContext.Lessons.AddRange(lessonA1, lessonA2, lessonB1);
        await dbContext.SaveChangesAsync();

        var questionA = CreateQuestion(lessonA1.Id, "Question A", now);
        var questionB = CreateQuestion(lessonB1.Id, "Question B", now);
        dbContext.Questions.AddRange(questionA, questionB);
        await dbContext.SaveChangesAsync();

        dbContext.Enrollments.AddRange(
            Enrollment.Create(studentA.Id, courseA.Id, now),
            Enrollment.Create(studentB.Id, courseB.Id, now));
        await dbContext.SaveChangesAsync();

        return new TestSeed(
            admin.Id,
            studentA.Id,
            studentB.Id,
            disabled.Id,
            courseA.Id,
            courseB.Id,
            lessonA1.Id,
            lessonA2.Id,
            lessonB1.Id,
            questionA.Id,
            questionB.Id,
            questionA.Options.Single(option => option.IsCorrect).Id,
            questionB.Options.Single(option => option.IsCorrect).Id);
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> users,
        string email,
        string fullName,
        UserRole role,
        DateTimeOffset now)
    {
        var user = new ApplicationUser { Email = email, UserName = email, CreatedAtUtc = now };
        user.Rename(fullName, now);
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role.ToIdentityName())).Succeeded);
        return user;
    }

    private static Question CreateQuestion(long lessonId, string text, DateTimeOffset now) =>
        Question.Create(
            lessonId,
            text,
            QuestionType.MultipleChoice,
            "Explanation",
            1,
            [
                new QuestionOptionDraft("Correct", true, 1),
                new QuestionOptionDraft("Incorrect", false, 2)
            ],
            now);
}
