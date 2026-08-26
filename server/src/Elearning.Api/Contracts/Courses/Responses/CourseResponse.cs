using Elearning.Domain;

namespace Elearning.Api.Contracts.Courses.Responses;

public sealed record CourseResponse(
    long Id,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    CourseStatus Status,
    int SortOrder,
    int LessonCount,
    int StudentCount,
    long Version);
