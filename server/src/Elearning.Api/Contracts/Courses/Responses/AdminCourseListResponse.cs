using Elearning.Domain;

namespace Elearning.Api.Contracts.Courses.Responses;

public sealed record AdminCourseListResponse(
    long Id,
    string Title,
    CourseStatus Status,
    int SortOrder,
    int LessonCount,
    int StudentCount,
    long Version);
