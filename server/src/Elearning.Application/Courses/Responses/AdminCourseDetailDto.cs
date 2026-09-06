using Elearning.Domain;

namespace Elearning.Application.Courses;

public sealed record AdminCourseDetailDto(
    long Id,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    CourseStatus Status,
    int SortOrder,
    long Version);
