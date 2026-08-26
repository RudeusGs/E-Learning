using Elearning.Domain;

namespace Elearning.Api.Contracts.Courses.Requests;

public sealed record CourseWriteRequest(
    string Title,
    string? Description,
    string? ThumbnailUrl,
    CourseStatus Status,
    int SortOrder,
    long? Version = null);
