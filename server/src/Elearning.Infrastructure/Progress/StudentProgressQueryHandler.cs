using Elearning.Application.Common;
using Elearning.Application.Common.Interfaces;
using Elearning.Application.Exceptions;
using Elearning.Application.Progress;
using Elearning.Domain;
using Elearning.Infrastructure.Authorization;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Progress;

public sealed class StudentProgressQueryHandler(
    ElearningDbContext dbContext,
    ICacheService cacheService) : IStudentProgressQueryHandler
{
    public async Task<StudentCourseProgressDto> ExecuteAsync(
        GetStudentProgressQuery query,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.StudentProgress(query.StudentId, query.CourseId);
        var cachedResult = await cacheService.GetAsync<StudentCourseProgressDto>(cacheKey, cancellationToken);
        if (cachedResult is not null)
        {
            return cachedResult;
        }

        await EnsureCourseAccessAsync(query.StudentId, query.CourseId, cancellationToken);
        var rows = await LoadProgressAsync(query.StudentId, query.CourseId, cancellationToken);
        var result = CreateResponse(query.CourseId, rows);

        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(1), cancellationToken);
        return result;
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
            .SelectMany(
                lesson => lesson.Progress
                    .Where(progress => progress.StudentId == studentId)
                    .DefaultIfEmpty(),
                (lesson, progress) => new StudentLessonProgressProjection(
                    lesson.Id,
                    lesson.Title,
                    progress == null ? null : progress.Status,
                    progress == null ? null : progress.StartedAtUtc,
                    progress == null ? null : progress.CompletedAtUtc))
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
