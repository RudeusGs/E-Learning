using Elearning.Domain;

namespace Elearning.Application.Courses;

public sealed record ListAdminCoursesQuery(
    int Limit,
    string? Cursor,
    string? Search,
    CourseStatus? Status,
    string? Sort);
