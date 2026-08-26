namespace Elearning.Application.Courses;

public sealed record StudentCourseDetailDto(
    long Id,
    string Title,
    string? Description,
    int CompletedLessons,
    int TotalLessons,
    int Percentage,
    IReadOnlyList<StudentLessonSummaryDto> Lessons);
