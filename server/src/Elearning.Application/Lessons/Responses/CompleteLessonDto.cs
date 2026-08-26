using Elearning.Application.Progress;

namespace Elearning.Application.Lessons;

public sealed record CompleteLessonDto(
    string Status,
    DateTimeOffset CompletedAtUtc,
    CourseProgressSummaryDto CourseProgress);
