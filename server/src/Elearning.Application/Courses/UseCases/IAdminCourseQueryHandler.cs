using Elearning.Application.Common;
using Elearning.Domain;

namespace Elearning.Application.Courses;

public interface IAdminCourseQueryHandler
{
    Task<CursorPage<AdminCourseListDto>> ExecuteAsync(
        ListAdminCoursesQuery request,
        CancellationToken cancellationToken);

    Task<AdminCourseDetailDto> ExecuteAsync(GetAdminCourseQuery query, CancellationToken cancellationToken);
}
