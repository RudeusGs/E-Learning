namespace Elearning.Application.Courses;

public sealed record StudentLessonSummaryDto(
    long Id,
    string Title,
    int SortOrder,
    string State,
    bool CanAccess);
