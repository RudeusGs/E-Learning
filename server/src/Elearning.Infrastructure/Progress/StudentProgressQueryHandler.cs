using Elearning.Application.Exceptions;
using Elearning.Application.Progress;
using Elearning.Domain;
using Elearning.Infrastructure.Authorization;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Progress;

public sealed class StudentProgressQueryHandler(
    ElearningDbContext dbContext,
    ActiveStudentPolicy activeStudentPolicy) : IStudentProgressQueryHandler
{
    public async Task<StudentCourseProgressDto> ExecuteAsync(
        GetStudentProgressQuery query,
        CancellationToken cancellationToken)
    {
        await activeStudentPolicy.EnsureSatisfiedAsync(query.StudentId, cancellationToken);
        await EnsureCourseAccessAsync(query.StudentId, query.CourseId, cancellationToken);
        var rows = await LoadProgressAsync(query.StudentId, query.CourseId, cancellationToken);
        return CreateResponse(query.CourseId, rows);
    }

    private async Task EnsureCourseAccessAsync(
        long studentId,
        long courseId,
        CancellationToken cancellationToken)
    {
        var hasAccess = await dbContext.Enrollments
            .AsNoTracking()
            .AnyAsync(enrollment =>
                enrollment.StudentId == studentId &&
                enrollment.CourseId == courseId &&
                enrollment.Status == EnrollmentStatus.Active &&
                enrollment.Course.Status == CourseStatus.Published,
                cancellationToken);
        if (!hasAccess)
        {
            throw new ResourceNotFoundException("Course");
        }
    }

    private Task<List<StudentLessonProgressProjection>> LoadProgressAsync(
        long studentId,
        long courseId,
        CancellationToken cancellationToken) =>
        dbContext.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.CourseId == courseId && lesson.Status == LessonStatus.Published)
            .OrderBy(lesson => lesson.SortOrder)
            .ThenBy(lesson => lesson.Id)
            .Select(lesson => new StudentLessonProgressProjection(
                lesson.Id,
                lesson.Title,
                lesson.Progress
                    .Where(progress => progress.StudentId == studentId)
                    .Select(progress => (LessonProgressStatus?)progress.Status)
                    .SingleOrDefault(),
                lesson.Progress
                    .Where(progress => progress.StudentId == studentId)
                    .Select(progress => (DateTimeOffset?)progress.StartedAtUtc)
                    .SingleOrDefault(),
                lesson.Progress
                    .Where(progress => progress.StudentId == studentId)
                    .Select(progress => progress.CompletedAtUtc)
                    .SingleOrDefault()))
            .ToListAsync(cancellationToken);

    private static StudentCourseProgressDto CreateResponse(
        long courseId,
        IReadOnlyList<StudentLessonProgressProjection> rows)
    {
        var details = rows.Select(row => new LessonProgressDetailDto(
            row.Id,
            row.Title,
            row.Status.ToContractValue(),
            row.StartedAtUtc,
            row.CompletedAtUtc)).ToList();
        var progress = CourseProgress.Calculate(
            rows.Count(row => row.Status == LessonProgressStatus.Completed),
            rows.Count);
        return new StudentCourseProgressDto(
            courseId,
            progress.CompletedLessons,
            progress.TotalLessons,
            progress.Percentage,
            details);
    }

    private sealed record StudentLessonProgressProjection(
        long Id,
        string Title,
        LessonProgressStatus? Status,
        DateTimeOffset? StartedAtUtc,
        DateTimeOffset? CompletedAtUtc);
}
