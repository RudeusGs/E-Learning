using Elearning.Domain;

namespace Elearning.Application.Courses;

public sealed record AdminCourseListDto(
    long Id,
    string Title,
    CourseStatus Status,
    int SortOrder,
    int LessonCount,
    int StudentCount,
    long Version);
