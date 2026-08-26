using Elearning.Application.Dashboard;
using Elearning.Domain;
using Elearning.Infrastructure.Identity;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Dashboard;

public sealed class AdminDashboardQueryHandler(
    ElearningDbContext dbContext) : IAdminDashboardQueryHandler
{
    private static readonly string StudentRoleName = UserRole.Student.ToIdentityName();

    public async Task<DashboardDto> ExecuteAsync(
        GetAdminDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var courseCount = await dbContext.Courses.AsNoTracking().CountAsync(
            course => course.Status != CourseStatus.Archived,
            cancellationToken);
        var studentCount = await dbContext.Users.AsNoTracking().CountAsync(
            user =>
                user.Status == AccountStatus.Active &&
                dbContext.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id &&
                    dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Name == StudentRoleName)),
            cancellationToken);
        var lessonCount = await dbContext.Lessons.AsNoTracking().CountAsync(
            lesson => lesson.Status != LessonStatus.Archived,
            cancellationToken);
        var completionCount = await dbContext.LessonProgress.AsNoTracking().CountAsync(
            progress => progress.Status == LessonProgressStatus.Completed,
            cancellationToken);
        return new DashboardDto(courseCount, studentCount, lessonCount, completionCount);
    }
}
