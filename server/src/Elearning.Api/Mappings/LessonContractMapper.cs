
using Elearning.Api.Contracts.Lessons.Responses;
using Elearning.Application.Lessons;
using ApiLessonWriteRequest = Elearning.Api.Contracts.Lessons.Requests.LessonWriteRequest;

namespace Elearning.Api.Mappings;

public static class LessonContractMapper
{
    public static IReadOnlyList<LessonAdminListResponse> ToResponse(
        this IReadOnlyList<LessonAdminListDto> lessons) =>
        lessons.Select(lesson => new LessonAdminListResponse(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Description,
            lesson.SortOrder,
            lesson.Status,
            lesson.Version)).ToList();

    public static LessonWriteRequest ToApplication(this ApiLessonWriteRequest request) =>
        new(
            request.Title,
            request.Description,
            request.ContentHtml,
            request.VideoUrl,
            request.SortOrder,
            request.Status,
            request.VideoDurationSeconds,
            request.Version);

    public static LessonAdminResponse ToResponse(this LessonAdminDto lesson) =>
        new(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Description,
            lesson.ContentHtml,
            lesson.Video is null ? null : new VideoResponse(
                lesson.Video.Provider,
                lesson.Video.ExternalId),
            lesson.VideoDurationSeconds,
            lesson.SortOrder,
            lesson.Status,
            lesson.Version);

    public static StudentLessonResponse ToResponse(this StudentLessonDto lesson) =>
        new(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Description,
            lesson.ContentHtml,
            lesson.Video is null ? null : new VideoResponse(
                lesson.Video.Provider,
                lesson.Video.ExternalId),
            lesson.ProgressStatus,
            lesson.VideoProgress.ToResponse(),
            lesson.Completion.ToResponse(),
            lesson.PreviousLessonId,
            lesson.NextLessonId,
            lesson.Questions.Select(ExerciseContractMapper.ToResponse).ToList());

    public static VideoProgressResponse ToResponse(this VideoProgressDto progress) =>
        new(
            progress.MaxPositionSeconds,
            progress.DurationSeconds,
            progress.Completed,
            progress.BlockedByQuestionId);

    public static LessonCompletionStateResponse ToResponse(
        this LessonCompletionStateDto completion) =>
        new(
            completion.VideoRequired,
            completion.VideoConfigured,
            completion.VideoCompleted,
            completion.ReinforcementUnlocked,
            completion.CheckpointTotal,
            completion.CheckpointPassed,
            completion.ReinforcementTotal,
            completion.ReinforcementPassed,
            completion.ReinforcementScorePercent,
            completion.RequiredScorePercent,
            completion.CanComplete);

    public static CompleteLessonResponse ToResponse(this CompleteLessonDto lesson) =>
        new(
            lesson.Status,
            lesson.CompletedAtUtc,
            lesson.CourseProgress.ToResponse(),
            lesson.NextLessonId);
}
