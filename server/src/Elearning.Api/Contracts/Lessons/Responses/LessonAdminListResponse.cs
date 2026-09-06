using Elearning.Domain;

namespace Elearning.Api.Contracts.Lessons.Responses;

public sealed record LessonAdminListResponse(
    long Id,
    long CourseId,
    string Title,
    string? Description,
    int SortOrder,
    LessonStatus Status,
    long Version);
