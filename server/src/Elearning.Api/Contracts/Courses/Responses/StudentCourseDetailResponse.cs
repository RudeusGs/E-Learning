namespace Elearning.Api.Contracts.Courses.Responses;

public sealed record StudentCourseDetailResponse(
    long Id,
    string Title,
    string? Description,
    int CompletedLessons,
    int TotalLessons,
    int Percentage,
    IReadOnlyList<StudentLessonSummaryResponse> Lessons);
