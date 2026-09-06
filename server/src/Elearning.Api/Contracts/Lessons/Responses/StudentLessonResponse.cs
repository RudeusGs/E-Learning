
using Elearning.Api.Contracts.Exercises.Responses;

namespace Elearning.Api.Contracts.Lessons.Responses;

public sealed record StudentLessonResponse(
    long Id,
    long CourseId,
    string Title,
    string? Description,
    string? ContentHtml,
    VideoResponse? Video,
    string ProgressStatus,
    VideoProgressResponse VideoProgress,
    LessonCompletionStateResponse Completion,
    long? PreviousLessonId,
    long? NextLessonId,
    IReadOnlyList<StudentQuestionResponse> Questions);
