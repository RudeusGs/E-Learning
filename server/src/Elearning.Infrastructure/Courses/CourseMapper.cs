using Elearning.Application.Courses;
using Elearning.Domain;

namespace Elearning.Infrastructure.Courses;

internal static class CourseMapper
{
    public static CourseDto ToDto(Course course, int lessonCount = 0, int studentCount = 0) =>
        new(
            course.Id,
            course.Title,
            course.Description,
            course.ThumbnailUrl,
            course.Status,
            course.SortOrder,
            lessonCount,
            studentCount,
            course.Version);
}
