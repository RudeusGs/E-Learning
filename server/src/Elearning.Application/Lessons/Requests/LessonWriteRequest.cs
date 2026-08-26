using Elearning.Domain;

namespace Elearning.Application.Lessons;

public sealed record LessonWriteRequest(
    string Title,
    string? Description,
    string? ContentHtml,
    string? VideoUrl,
    int SortOrder,
    LessonStatus Status,
    long? Version = null);
