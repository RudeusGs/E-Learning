namespace Elearning.Application.Courses;

public sealed record StudentCourseDto(
    long Id,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    int TotalLessons,
    int CompletedLessons,
    int Percentage);
