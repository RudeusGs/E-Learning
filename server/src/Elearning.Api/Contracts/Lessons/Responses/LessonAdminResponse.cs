using Elearning.Domain;

namespace Elearning.Api.Contracts.Lessons.Responses;

public sealed record LessonAdminResponse(
    long Id,
    long CourseId,
    string Title,
    string? Description,
    string? ContentHtml,
    VideoResponse? Video,
    int SortOrder,
    LessonStatus Status,
    long Version);
