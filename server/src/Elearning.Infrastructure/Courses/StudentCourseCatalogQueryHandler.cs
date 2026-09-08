using Elearning.Application.Common;
using Elearning.Application.Common.Interfaces;
using Elearning.Application.Courses;
using Elearning.Application.Exceptions;
using Elearning.Domain;
using Elearning.Infrastructure.Authorization;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Courses;

public sealed class StudentCourseCatalogQueryHandler(
    ElearningDbContext dbContext,
    ICacheService cacheService) : IStudentCourseCatalogQueryHandler
{
    public async Task<CursorPage<StudentCourseDto>> ExecuteAsync(
        ListStudentCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var limit = RequestValidation.ValidateLimit(request.Limit);
        var progress = NormalizeProgressFilter(request.Progress);

        var cacheKey = CacheKeys.StudentCourses(request.StudentId, limit, request.Cursor, progress);
        var cachedResult = await cacheService.GetAsync<CursorPage<StudentCourseDto>>(cacheKey, cancellationToken);
        if (cachedResult is not null)
        {
            return cachedResult;
        }

        var position = CursorCodec.Decode<StudentCourseCursor>(request.Cursor);
        var rows = await LoadCoursesAsync(
            request.StudentId,
            limit,
            position,
            progress,
            cancellationToken);

        var result = CreatePage(rows, limit);
        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
        return result;
    }

    private Task<List<StudentCourseProjection>> LoadCoursesAsync(
        long studentId,
        int limit,
        StudentCourseCursor? position,
        string? progress,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment =>
                enrollment.StudentId == studentId &&
                enrollment.Status == EnrollmentStatus.Active &&
                enrollment.Course.Status == CourseStatus.Published);

        query = progress switch
        {
            StudentCourseProgressFilters.NotStarted => query.Where(enrollment =>
                !enrollment.Course.Lessons.Any(lesson =>
                    lesson.Status == LessonStatus.Published &&
                    lesson.Progress.Any(item =>
                        item.StudentId == studentId &&
                        item.Status == LessonProgressStatus.Completed))),

            StudentCourseProgressFilters.Completed => query.Where(enrollment =>
                enrollment.Course.Lessons.Any(lesson =>
                    lesson.Status == LessonStatus.Published) &&
                !enrollment.Course.Lessons.Any(lesson =>
                    lesson.Status == LessonStatus.Published &&
                    !lesson.Progress.Any(item =>
                        item.StudentId == studentId &&
                        item.Status == LessonProgressStatus.Completed))),

            StudentCourseProgressFilters.InProgress => query.Where(enrollment =>
                enrollment.Course.Lessons.Any(lesson =>
                    lesson.Status == LessonStatus.Published &&
                    lesson.Progress.Any(item =>
                        item.StudentId == studentId &&
                        item.Status == LessonProgressStatus.Completed)) &&
                enrollment.Course.Lessons.Any(lesson =>
                    lesson.Status == LessonStatus.Published &&
                    !lesson.Progress.Any(item =>
                        item.StudentId == studentId &&
                        item.Status == LessonProgressStatus.Completed))),

            _ => query,
        };

        if (position is not null)
        {
            query = query.Where(enrollment =>
                enrollment.Course.SortOrder > position.Value.SortOrder ||
                (enrollment.Course.SortOrder == position.Value.SortOrder &&
                 enrollment.CourseId > position.Value.CourseId));
        }

        return query
            .OrderBy(enrollment => enrollment.Course.SortOrder)
            .ThenBy(enrollment => enrollment.CourseId)
            .Take(limit + 1)
            .Select(enrollment => new StudentCourseProjection(
                enrollment.CourseId,
                enrollment.Course.SortOrder,
                enrollment.Course.Title,
                enrollment.Course.Description,
                enrollment.Course.ThumbnailUrl,
                enrollment.Course.Lessons.Count(lesson => lesson.Status == LessonStatus.Published),
                enrollment.Course.Lessons.Count(lesson =>
                    lesson.Status == LessonStatus.Published &&
                    lesson.Progress.Any(item =>
                        item.StudentId == studentId &&
                        item.Status == LessonProgressStatus.Completed))))
            .ToListAsync(cancellationToken);
    }

    private static string? NormalizeProgressFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            StudentCourseProgressFilters.InProgress => normalized,
            StudentCourseProgressFilters.NotStarted => normalized,
            StudentCourseProgressFilters.Completed => normalized,
            _ => throw new RequestValidationException(
                "Invalid progress filter",
                $"progress must be one of: {StudentCourseProgressFilters.InProgress}, " +
                $"{StudentCourseProgressFilters.NotStarted}, {StudentCourseProgressFilters.Completed}."),
        };
    }

    private static CursorPage<StudentCourseDto> CreatePage(
        List<StudentCourseProjection> rows,
        int limit)
    {
        var hasMore = rows.Count > limit;
        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        var items = rows.Select(row =>
        {
            var progress = CourseProgress.Calculate(row.CompletedLessons, row.TotalLessons);
            return new StudentCourseDto(
                row.Id,
                row.Title,
                row.Description,
                row.ThumbnailUrl,
                progress.TotalLessons,
                progress.CompletedLessons,
                progress.Percentage);
        }).ToList();

        var nextCursor = hasMore && rows.Count > 0
            ? CursorCodec.Encode(new StudentCourseCursor(rows[^1].SortOrder, rows[^1].Id))
            : null;

        return new CursorPage<StudentCourseDto>(items, nextCursor, hasMore);
    }

    private sealed record StudentCourseProjection(
        long Id,
        int SortOrder,
        string Title,
        string? Description,
        string? ThumbnailUrl,
        int TotalLessons,
        int CompletedLessons);

    private readonly record struct StudentCourseCursor(int SortOrder, long CourseId);
}
