using Elearning.Domain;

namespace Elearning.Api.Contracts.Courses.Responses;

public sealed record AdminCourseDetailResponse(
    long Id,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    CourseStatus Status,
    int SortOrder,
    long Version);
