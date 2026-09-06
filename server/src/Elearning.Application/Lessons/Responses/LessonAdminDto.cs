
using Elearning.Domain;

namespace Elearning.Application.Lessons;

public sealed record LessonAdminDto(
    long Id,
    long CourseId,
    string Title,
    string? Description,
    string? ContentHtml,
    VideoDto? Video,
    int? VideoDurationSeconds,
    int SortOrder,
    LessonStatus Status,
    long Version);
