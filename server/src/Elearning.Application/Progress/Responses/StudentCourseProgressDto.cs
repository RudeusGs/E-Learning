namespace Elearning.Application.Progress;

public sealed record StudentCourseProgressDto(
    long CourseId,
    int CompletedLessons,
    int TotalLessons,
    int Percentage,
    IReadOnlyList<LessonProgressDetailDto> Lessons);
