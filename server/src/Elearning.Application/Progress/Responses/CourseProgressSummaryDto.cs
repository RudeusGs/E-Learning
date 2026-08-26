namespace Elearning.Application.Progress;

public sealed record CourseProgressSummaryDto(int CompletedLessons, int TotalLessons, int Percentage);
