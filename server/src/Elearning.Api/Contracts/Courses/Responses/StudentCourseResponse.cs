namespace Elearning.Api.Contracts.Courses.Responses;

public sealed record StudentCourseResponse(
    long Id,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    int TotalLessons,
    int CompletedLessons,
    int Percentage);
