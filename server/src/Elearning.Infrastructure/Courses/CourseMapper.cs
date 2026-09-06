using Elearning.Application.Courses;
using Elearning.Domain;

namespace Elearning.Infrastructure.Courses;

internal static class CourseMapper
{
    public static AdminCourseDetailDto ToDetailDto(Course course) =>
        new(
            course.Id,
            course.Title,
            course.Description,
            course.ThumbnailUrl,
            course.Status,
            course.SortOrder,
            course.Version);
}
