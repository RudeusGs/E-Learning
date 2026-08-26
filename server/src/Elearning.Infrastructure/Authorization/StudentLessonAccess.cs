namespace Elearning.Infrastructure.Authorization;

public sealed record StudentLessonAccess(
    long LessonId,
    long CourseId,
    int SortOrder,
    long? PreviousLessonId);
