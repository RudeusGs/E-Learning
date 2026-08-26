namespace Elearning.Application.Progress;

public sealed record ProgressRowDto(
    long StudentId,
    string StudentName,
    long CourseId,
    string CourseTitle,
    int CompletedLessons,
    int TotalLessons,
    int Percentage);
