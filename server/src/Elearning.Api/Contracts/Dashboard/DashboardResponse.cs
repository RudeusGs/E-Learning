namespace Elearning.Api.Contracts.Dashboard;

public sealed record DashboardResponse(
    int CourseCount,
    int StudentCount,
    int LessonCount,
    int LessonCompletionCount);
