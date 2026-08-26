namespace Elearning.Api.Contracts.Progress.Responses;

public sealed record ProgressRowResponse(
    long StudentId,
    string StudentName,
    long CourseId,
    string CourseTitle,
    int CompletedLessons,
    int TotalLessons,
    int Percentage);
