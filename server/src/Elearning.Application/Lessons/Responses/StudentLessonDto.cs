using Elearning.Application.Exercises;

namespace Elearning.Application.Lessons;

public sealed record StudentLessonDto(
    long Id,
    long CourseId,
    string Title,
    string? Description,
    string? ContentHtml,
    VideoDto? Video,
    string ProgressStatus,
    long? PreviousLessonId,
    long? NextLessonId,
    IReadOnlyList<StudentQuestionDto> Questions);
