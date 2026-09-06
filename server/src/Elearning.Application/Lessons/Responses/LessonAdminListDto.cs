using Elearning.Domain;

namespace Elearning.Application.Lessons;

public sealed record LessonAdminListDto(
    long Id,
    long CourseId,
    string Title,
    string? Description,
    int SortOrder,
    LessonStatus Status,
    long Version);
