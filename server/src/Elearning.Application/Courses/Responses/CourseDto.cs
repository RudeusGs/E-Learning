using Elearning.Domain;

namespace Elearning.Application.Courses;

public sealed record CourseDto(
    long Id,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    CourseStatus Status,
    int SortOrder,
    int LessonCount,
    int StudentCount,
    long Version);
