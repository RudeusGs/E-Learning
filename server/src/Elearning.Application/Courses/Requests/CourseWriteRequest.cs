using Elearning.Domain;

namespace Elearning.Application.Courses;

public sealed record CourseWriteRequest(
    string Title,
    string? Description,
    string? ThumbnailUrl,
    CourseStatus Status,
    int SortOrder,
    long? Version = null);
