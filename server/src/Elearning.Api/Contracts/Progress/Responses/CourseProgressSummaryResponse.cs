namespace Elearning.Api.Contracts.Progress.Responses;

public sealed record CourseProgressSummaryResponse(
    int CompletedLessons,
    int TotalLessons,
    int Percentage);
