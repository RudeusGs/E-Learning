using Elearning.Application.Common;
using Elearning.Application.Courses;
using Elearning.Domain;
using Elearning.Infrastructure.Authorization;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Courses;

public sealed class StudentCourseCatalogQueryHandler(
    ElearningDbContext dbContext,
    ActiveStudentPolicy activeStudentPolicy) : IStudentCourseCatalogQueryHandler
{
    public async Task<CursorPage<StudentCourseDto>> ExecuteAsync(
        ListStudentCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var limit = RequestValidation.ValidateLimit(request.Limit);
        var position = CursorCodec.Decode<StudentCourseCursor>(request.Cursor);
        await activeStudentPolicy.EnsureSatisfiedAsync(request.StudentId, cancellationToken);
        var rows = await LoadCoursesAsync(request.StudentId, limit, position, cancellationToken);
        return CreatePage(rows, limit);
    }

    private Task<List<StudentCourseProjection>> LoadCoursesAsync(
        long studentId,
        int limit,
        StudentCourseCursor? position,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment =>
                enrollment.StudentId == studentId &&
                enrollment.Status == EnrollmentStatus.Active &&
                enrollment.Course.Status == CourseStatus.Published);

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
                    lesson.Progress.Any(progress =>
                        progress.StudentId == studentId &&
                        progress.Status == LessonProgressStatus.Completed))))
            .ToListAsync(cancellationToken);
    }

    private static CursorPage<StudentCourseDto> CreatePage(List<StudentCourseProjection> rows, int limit)
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
