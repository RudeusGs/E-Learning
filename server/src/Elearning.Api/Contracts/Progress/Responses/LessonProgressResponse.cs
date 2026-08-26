namespace Elearning.Api.Contracts.Progress.Responses;

public sealed record LessonProgressResponse(
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);
