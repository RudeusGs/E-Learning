namespace Elearning.Application.Progress;

public sealed record LessonProgressDetailDto(
    long LessonId,
    string Title,
    string Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);
