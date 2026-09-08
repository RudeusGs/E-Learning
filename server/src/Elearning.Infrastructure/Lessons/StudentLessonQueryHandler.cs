
using Elearning.Application.Exercises;
using Elearning.Application.Lessons;
using Elearning.Application.Progress;
using Elearning.Domain;
using Elearning.Infrastructure.Authorization;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Lessons;

public sealed class StudentLessonQueryHandler(
    ElearningDbContext dbContext,
    StudentLessonAccessPolicy accessPolicy) : IStudentLessonQueryHandler
{
    public async Task<StudentLessonDto> ExecuteAsync(
        GetStudentLessonQuery query,
        CancellationToken cancellationToken)
    {
        var studentId = query.StudentId;
        var lessonId = query.LessonId;

        var access = await accessPolicy.AuthorizeAsync(
            studentId,
            lessonId,
            cancellationToken);

        var lesson = await LoadLessonAsync(
            studentId,
            lessonId,
            cancellationToken);

        var questions = await LoadQuestionsAsync(
            studentId,
            lessonId,
            cancellationToken);

        var completion = LessonCompletionStateCalculator.Calculate(
            questions,
            lesson.VideoProvider is not null,
            lesson.VideoDurationSeconds,
            lesson.Progress,
            lesson.VideoCompletedAtUtc);

        var nextLessonId = lesson.Progress == LessonProgressStatus.Completed
            ? await FindNextLessonIdAsync(
                access.CourseId,
                access.SortOrder,
                access.LessonId,
                cancellationToken)
            : null;

        return CreateResponse(
            lesson,
            questions,
            completion,
            access.PreviousLessonId,
            nextLessonId);
    }

    private Task<StudentLessonProjection> LoadLessonAsync(
        long studentId,
        long lessonId,
        CancellationToken cancellationToken) =>
        dbContext.Lessons
            .AsNoTracking()
            .Where(candidate => candidate.Id == lessonId)
            .Select(candidate => new StudentLessonProjection(
                candidate.Id,
                candidate.CourseId,
                candidate.Title,
                candidate.Description,
                candidate.ContentHtml,
                candidate.VideoProvider,
                candidate.VideoExternalId,
                candidate.VideoDurationSeconds,
                candidate.Progress
                    .Where(progress => progress.StudentId == studentId)
                    .Select(progress => (LessonProgressStatus?)progress.Status)
                    .SingleOrDefault(),
                candidate.Progress
                    .Where(progress => progress.StudentId == studentId)
                    .Select(progress => (int?)progress.VideoMaxPositionSeconds)
                    .SingleOrDefault() ?? 0,
                candidate.Progress
                    .Where(progress => progress.StudentId == studentId)
                    .Select(progress => progress.VideoCompletedAtUtc)
                    .SingleOrDefault()))
            .SingleAsync(cancellationToken);

    private static StudentLessonDto CreateResponse(
        StudentLessonProjection lesson,
        IReadOnlyList<StudentQuestionDto> questions,
        LessonCompletionStateDto completion,
        long? previousLessonId,
        long? nextLessonId) =>
        new(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Description,
            lesson.ContentHtml,
            lesson.VideoProvider is not null &&
            lesson.VideoExternalId is not null
                ? new VideoDto(
                    lesson.VideoProvider.Value,
                    lesson.VideoExternalId)
                : null,
            lesson.Progress.ToContractValue(),
            new VideoProgressDto(
                lesson.VideoMaxPositionSeconds,
                lesson.VideoDurationSeconds,
                lesson.VideoCompletedAtUtc is not null,
                null),
            completion,
            previousLessonId,
            nextLessonId,
            questions);

    private Task<List<StudentQuestionDto>> LoadQuestionsAsync(
        long studentId,
        long lessonId,
        CancellationToken cancellationToken) =>
        dbContext.Questions
            .AsNoTracking()
            .Where(question => question.LessonId == lessonId)
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.Id)
            .Select(question => new StudentQuestionDto(
                question.Id,
                question.Text,
                question.Type,
                question.Placement,
                question.VideoTimestampSeconds,
                question.Placement == QuestionPlacement.VideoCheckpoint
                    ? dbContext.StudentAnswers.Any(answer =>
                        answer.StudentId == studentId &&
                        answer.QuestionId == question.Id &&
                        answer.IsCorrect)
                    : dbContext.StudentAnswers
                        .Where(answer =>
                            answer.StudentId == studentId &&
                            answer.QuestionId == question.Id)
                        .OrderByDescending(answer => answer.AnsweredAtUtc)
                        .ThenByDescending(answer => answer.Id)
                        .Select(answer => answer.IsCorrect)
                        .FirstOrDefault(),
                question.Options
                    .OrderBy(option => option.SortOrder)
                    .Select(option => new StudentQuestionOptionDto(
                        option.Id,
                        option.Content))
                    .ToList()))
            .ToListAsync(cancellationToken);

    private Task<long?> FindNextLessonIdAsync(
        long courseId,
        int sortOrder,
        long lessonId,
        CancellationToken cancellationToken) =>
        dbContext.Lessons
            .AsNoTracking()
            .Where(candidate =>
                candidate.CourseId == courseId &&
                candidate.Status == LessonStatus.Published &&
                (candidate.SortOrder > sortOrder ||
                 (candidate.SortOrder == sortOrder &&
                  candidate.Id > lessonId)))
            .OrderBy(candidate => candidate.SortOrder)
            .ThenBy(candidate => candidate.Id)
            .Select(candidate => (long?)candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private sealed record StudentLessonProjection(
        long Id,
        long CourseId,
        string Title,
        string? Description,
        string? ContentHtml,
        VideoProvider? VideoProvider,
        string? VideoExternalId,
        int? VideoDurationSeconds,
        LessonProgressStatus? Progress,
        int VideoMaxPositionSeconds,
        DateTimeOffset? VideoCompletedAtUtc);
}
