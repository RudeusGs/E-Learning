using Elearning.Api.Contracts.Progress.Responses;

namespace Elearning.Api.Contracts.Lessons.Responses;

public sealed record CompleteLessonResponse(
    string Status,
    DateTimeOffset CompletedAtUtc,
    CourseProgressSummaryResponse CourseProgress);
