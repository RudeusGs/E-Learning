using Elearning.Api.Contracts.Lessons.Responses;
using Elearning.Application.Lessons;
using ApiLessonWriteRequest = Elearning.Api.Contracts.Lessons.Requests.LessonWriteRequest;

namespace Elearning.Api.Mappings;

public static class LessonContractMapper
{
    public static IReadOnlyList<LessonAdminResponse> ToResponse(
        this IReadOnlyList<LessonAdminDto> lessons) =>
        lessons.Select(ToResponse).ToList();

    public static LessonWriteRequest ToApplication(this ApiLessonWriteRequest request) =>
        new(
            request.Title,
            request.Description,
            request.ContentHtml,
            request.VideoUrl,
            request.SortOrder,
            request.Status,
            request.Version);

    public static LessonAdminResponse ToResponse(this LessonAdminDto lesson) =>
        new(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Description,
            lesson.ContentHtml,
            lesson.Video is null ? null : new VideoResponse(lesson.Video.Provider, lesson.Video.ExternalId),
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
            lesson.Video is null ? null : new VideoResponse(lesson.Video.Provider, lesson.Video.ExternalId),
            lesson.ProgressStatus,
            lesson.PreviousLessonId,
            lesson.NextLessonId,
            lesson.Questions.Select(ExerciseContractMapper.ToResponse).ToList());

    public static CompleteLessonResponse ToResponse(this CompleteLessonDto lesson) =>
        new(lesson.Status, lesson.CompletedAtUtc, lesson.CourseProgress.ToResponse());
}
