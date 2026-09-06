using Elearning.Application.Common;
using Elearning.Application.Students;
using Elearning.Domain;
using Elearning.Infrastructure.Identity;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Students;

public sealed class AdminStudentQueryHandler(
    ElearningDbContext dbContext,
    StudentAccountStore students) : IAdminStudentQueryHandler
{
    public async Task<CursorPage<StudentListItemDto>> ExecuteAsync(
        ListStudentsQuery request,
        CancellationToken cancellationToken)
    {
        var limit = RequestValidation.ValidateLimit(request.Limit);
        var search = RequestValidation.NormalizeSearch(request.Search);
        var position = CursorCodec.Decode<StudentCursor>(request.Cursor);
        var query = BuildListQuery(position, search, request.Status);
        var items = await LoadListItemsAsync(query, limit, cancellationToken);
        return CreatePage(items, limit);
    }

    public async Task<StudentDetailDto> ExecuteAsync(
        GetStudentQuery query,
        CancellationToken cancellationToken)
    {
        var student = await students.FindAsync(
            query.StudentId,
            tracked: false,
            cancellationToken);

        var enrollments = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.StudentId == query.StudentId)
            .OrderBy(enrollment => enrollment.Id)
            .Select(enrollment => new StudentEnrollmentDto(
                enrollment.Id,
                enrollment.CourseId,
                enrollment.Course.Title,
                enrollment.Status))
            .ToListAsync(cancellationToken);

        return StudentMapper.ToDetail(student, enrollments);
    }

    private IQueryable<ApplicationUser> BuildListQuery(
        StudentCursor? position,
        string? search,
        AccountStatus? status)
    {
        var query = students.Query(tracked: false);

        if (position is not null)
        {
            query = query.Where(user => user.Id > position.Value.Id);
        }

        if (search is not null)
        {
            query = query.Where(user =>
                EF.Functions.ILike(user.FullName, $"%{search}%") ||
                (user.Email != null && EF.Functions.ILike(user.Email, $"%{search}%")));
        }

        if (status is not null)
        {
            query = query.Where(user => user.Status == status);
        }

        return query;
    }

    private async Task<List<StudentListItemDto>> LoadListItemsAsync(
        IQueryable<ApplicationUser> query,
        int limit,
        CancellationToken cancellationToken) =>
        await query
            .OrderBy(user => user.Id)
            .Take(limit + 1)
            .Select(user => new StudentListItemDto(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                dbContext.Enrollments.Count(enrollment =>
                    enrollment.StudentId == user.Id &&
                    enrollment.Status == EnrollmentStatus.Active),
                user.Status))
            .ToListAsync(cancellationToken);

    private static CursorPage<StudentListItemDto> CreatePage(
        List<StudentListItemDto> items,
        int limit)
    {
        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var nextCursor = hasMore && items.Count > 0
            ? CursorCodec.Encode(new StudentCursor(items[^1].Id))
            : null;

        return new CursorPage<StudentListItemDto>(items, nextCursor, hasMore);
    }

    private readonly record struct StudentCursor(long Id);
}
