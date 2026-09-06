using Elearning.Application.Common;
using Elearning.Application.Courses;
using Elearning.Application.Exceptions;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Courses;

public sealed class AdminCourseQueryHandler(ElearningDbContext dbContext) : IAdminCourseQueryHandler
{
    public async Task<CursorPage<AdminCourseListDto>> ExecuteAsync(
        ListAdminCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeRequest(
            request.Limit,
            request.Cursor,
            request.Search,
            request.Status,
            request.Sort);
        var query = BuildQuery(normalized);
        var items = await LoadPageAsync(query, normalized.Limit, cancellationToken);
        return CreatePage(items, normalized);
    }

    private static CourseListRequest NormalizeRequest(
        int limit,
        string? cursor,
        string? search,
        CourseStatus? status,
        string? sort)
    {
        var normalizedLimit = RequestValidation.ValidateLimit(limit);
        var normalizedSearch = RequestValidation.NormalizeSearch(search);
        sort ??= CourseSortOptions.SortOrderAscending;
        if (sort is not (CourseSortOptions.SortOrderAscending or CourseSortOptions.SortOrderDescending))
        {
            throw new RequestValidationException(
                "Invalid sort",
                "Supported course sorts are sortOrder and -sortOrder.");
        }

        var position = CursorCodec.Decode<CourseListCursor>(cursor);
        if (position is not null && position.Value.Sort != sort)
        {
            throw new RequestValidationException(
                "Invalid cursor",
                "The cursor does not match the requested sort.");
        }

        return new CourseListRequest(normalizedLimit, normalizedSearch, status, sort, position);
    }

    private IQueryable<Course> BuildQuery(CourseListRequest request)
    {
        var query = dbContext.Courses.AsNoTracking();
        if (request.Search is not null)
        {
            query = query.Where(course => EF.Functions.ILike(course.Title, $"%{request.Search}%"));
        }

        if (request.Status is not null)
        {
            query = query.Where(course => course.Status == request.Status);
        }

        if (request.Position is not null)
        {
            var current = request.Position.Value;
            query = request.Sort == CourseSortOptions.SortOrderAscending
                ? query.Where(course =>
                    course.SortOrder > current.SortOrder ||
                    (course.SortOrder == current.SortOrder && course.Id > current.Id))
                : query.Where(course =>
                    course.SortOrder < current.SortOrder ||
                    (course.SortOrder == current.SortOrder && course.Id < current.Id));
        }

        return request.Sort == CourseSortOptions.SortOrderAscending
            ? query.OrderBy(course => course.SortOrder).ThenBy(course => course.Id)
            : query.OrderByDescending(course => course.SortOrder).ThenByDescending(course => course.Id);
    }

    private static Task<List<AdminCourseListDto>> LoadPageAsync(
        IQueryable<Course> query,
        int limit,
        CancellationToken cancellationToken) =>
        query
            .Take(limit + 1)
            .Select(course => new AdminCourseListDto(
                course.Id,
                course.Title,
                course.Status,
                course.SortOrder,
                course.Lessons.Count(lesson => lesson.Status != LessonStatus.Archived),
                course.Enrollments.Count(enrollment => enrollment.Status == EnrollmentStatus.Active),
                course.Version))
            .ToListAsync(cancellationToken);

    private static CursorPage<AdminCourseListDto> CreatePage(List<AdminCourseListDto> items, CourseListRequest request)
    {
        var hasMore = items.Count > request.Limit;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var nextCursor = hasMore && items.Count > 0
            ? CursorCodec.Encode(new CourseListCursor(request.Sort, items[^1].SortOrder, items[^1].Id))
            : null;
        return new CursorPage<AdminCourseListDto>(items, nextCursor, hasMore);
    }

    public async Task<AdminCourseDetailDto> ExecuteAsync(GetAdminCourseQuery query, CancellationToken cancellationToken) =>
        await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Id == query.CourseId)
            .Select(course => new AdminCourseDetailDto(
                course.Id,
                course.Title,
                course.Description,
                course.ThumbnailUrl,
                course.Status,
                course.SortOrder,
                course.Version))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new ResourceNotFoundException("Course");

    private readonly record struct CourseListCursor(string Sort, int SortOrder, long Id);
    private sealed record CourseListRequest(
        int Limit,
        string? Search,
        CourseStatus? Status,
        string Sort,
        CourseListCursor? Position);
}
