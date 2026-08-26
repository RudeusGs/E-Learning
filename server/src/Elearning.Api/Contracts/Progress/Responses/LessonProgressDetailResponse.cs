namespace Elearning.Api.Contracts.Progress.Responses;

public sealed record LessonProgressDetailResponse(
    long LessonId,
    string Title,
    string Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);
