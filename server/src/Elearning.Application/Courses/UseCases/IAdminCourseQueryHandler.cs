using Elearning.Application.Common;
using Elearning.Domain;

namespace Elearning.Application.Courses;

public interface IAdminCourseQueryHandler
{
    Task<CursorPage<CourseDto>> ExecuteAsync(
        ListAdminCoursesQuery request,
        CancellationToken cancellationToken);

    Task<CourseDto> ExecuteAsync(GetAdminCourseQuery query, CancellationToken cancellationToken);
}
