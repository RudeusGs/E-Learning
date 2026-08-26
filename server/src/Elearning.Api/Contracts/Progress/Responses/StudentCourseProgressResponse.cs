namespace Elearning.Api.Contracts.Progress.Responses;

public sealed record StudentCourseProgressResponse(
    long CourseId,
    int CompletedLessons,
    int TotalLessons,
    int Percentage,
    IReadOnlyList<LessonProgressDetailResponse> Lessons);
