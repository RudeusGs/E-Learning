namespace Elearning.Application.Progress;

public sealed record GetAdminProgressQuery(
    int Limit,
    string? Cursor,
    long? StudentId,
    long? CourseId,
    string? Search);
