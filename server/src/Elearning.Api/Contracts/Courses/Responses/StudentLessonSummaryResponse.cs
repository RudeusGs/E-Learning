namespace Elearning.Api.Contracts.Courses.Responses;

public sealed record StudentLessonSummaryResponse(
    long Id,
    string Title,
    int SortOrder,
    string State,
    bool CanAccess);
