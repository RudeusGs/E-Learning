namespace Elearning.Application.Progress;

public sealed record LessonProgressDto(
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);
