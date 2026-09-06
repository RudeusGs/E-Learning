
using Elearning.Domain;

namespace Elearning.Api.Contracts.Lessons.Requests;

public sealed record LessonWriteRequest(
    string Title,
    string? Description,
    string? ContentHtml,
    string? VideoUrl,
    int SortOrder,
    LessonStatus Status,
    int? VideoDurationSeconds = null,
    long? Version = null);
