using Elearning.Application.Common;

namespace Elearning.Application.Courses;

public interface IStudentCourseCatalogQueryHandler
{
    Task<CursorPage<StudentCourseDto>> ExecuteAsync(
        ListStudentCoursesQuery request,
        CancellationToken cancellationToken);
}
