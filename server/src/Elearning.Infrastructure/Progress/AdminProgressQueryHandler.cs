using Elearning.Application;
using Elearning.Application.Common;
using Elearning.Application.Progress;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Progress;

public sealed class AdminProgressQueryHandler(
    ElearningDbContext dbContext) : IAdminProgressQueryHandler
{
    public async Task<CursorPage<ProgressRowDto>> ExecuteAsync(
        GetAdminProgressQuery request,
        CancellationToken cancellationToken)
    {
        var limit = RequestValidation.ValidateLimit(request.Limit);
        var search = RequestValidation.NormalizeSearch(request.Search);
        var position = CursorCodec.Decode<ProgressCursor>(request.Cursor);
        var query = BuildQuery(position, request.StudentId, request.CourseId, search);
        var rows = await LoadPageAsync(query, limit, cancellationToken);
        return CreatePage(rows, limit);
    }

    private IQueryable<Enrollment> BuildQuery(
        ProgressCursor? position,
        long? studentId,
        long? courseId,
        string? search)
    {
        var query = dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.Status == EnrollmentStatus.Active);
        if (position is not null)
        {
            query = query.Where(enrollment => enrollment.Id > position.Value.EnrollmentId);
        }

        if (studentId is not null)
        {
            query = query.Where(enrollment => enrollment.StudentId == studentId);
        }

        if (courseId is not null)
        {
            query = query.Where(enrollment => enrollment.CourseId == courseId);
        }

        if (search is not null)
        {
            query = query.Where(enrollment =>
                dbContext.Users.Any(student =>
                    student.Id == enrollment.StudentId &&
                    EF.Functions.ILike(student.FullName, $"%{search}%")) ||
                EF.Functions.ILike(enrollment.Course.Title, $"%{search}%"));
        }

        return query;
    }

    private Task<List<ProgressProjection>> LoadPageAsync(
        IQueryable<Enrollment> query,
        int limit,
        CancellationToken cancellationToken) =>
        query
            .OrderBy(enrollment => enrollment.Id)
            .Take(limit + 1)
            .Select(enrollment => new ProgressProjection(
                enrollment.Id,
                enrollment.StudentId,
                dbContext.Users.Where(student => student.Id == enrollment.StudentId)
                    .Select(student => student.FullName)
                    .Single(),
                enrollment.CourseId,
                enrollment.Course.Title,
                enrollment.Course.Lessons.Count(lesson => lesson.Status == LessonStatus.Published),
                enrollment.Course.Lessons.Count(lesson =>
                    lesson.Status == LessonStatus.Published &&
                    lesson.Progress.Any(progress =>
                        progress.StudentId == enrollment.StudentId &&
                        progress.Status == LessonProgressStatus.Completed))))
            .ToListAsync(cancellationToken);

    private static CursorPage<ProgressRowDto> CreatePage(List<ProgressProjection> rows, int limit)
    {
        var hasMore = rows.Count > limit;
        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        var items = rows.Select(row =>
        {
            var progress = CourseProgress.Calculate(row.CompletedLessons, row.TotalLessons);
            return new ProgressRowDto(
                row.StudentId,
                row.StudentName,
                row.CourseId,
                row.CourseTitle,
                progress.CompletedLessons,
                progress.TotalLessons,
                progress.Percentage);
        }).ToList();
        var nextCursor = hasMore && rows.Count > 0
            ? CursorCodec.Encode(new ProgressCursor(rows[^1].EnrollmentId))
            : null;
        return new CursorPage<ProgressRowDto>(items, nextCursor, hasMore);
    }

    private sealed record ProgressProjection(
        long EnrollmentId,
        long StudentId,
        string StudentName,
        long CourseId,
        string CourseTitle,
        int TotalLessons,
        int CompletedLessons);

    private readonly record struct ProgressCursor(long EnrollmentId);
}
