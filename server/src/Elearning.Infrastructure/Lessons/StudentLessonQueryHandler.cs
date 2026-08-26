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
        var access = await accessPolicy.AuthorizeAsync(studentId, lessonId, cancellationToken);
        var lesson = await LoadLessonAsync(lessonId, cancellationToken);
        var questions = await LoadQuestionsAsync(lessonId, cancellationToken);
        var progress = await LoadProgressAsync(studentId, lessonId, cancellationToken);
        var nextLessonId = progress == LessonProgressStatus.Completed
            ? await FindNextLessonIdAsync(access.CourseId, access.SortOrder, access.LessonId, cancellationToken)
            : null;
        return CreateResponse(lesson, questions, progress, access.PreviousLessonId, nextLessonId);
    }

    private Task<StudentLessonProjection> LoadLessonAsync(long lessonId, CancellationToken cancellationToken) =>
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
                candidate.VideoExternalId))
            .SingleAsync(cancellationToken);

    private Task<LessonProgressStatus?> LoadProgressAsync(
        long studentId,
        long lessonId,
        CancellationToken cancellationToken) =>
        dbContext.LessonProgress
            .AsNoTracking()
            .Where(candidate => candidate.StudentId == studentId && candidate.LessonId == lessonId)
            .Select(candidate => (LessonProgressStatus?)candidate.Status)
            .SingleOrDefaultAsync(cancellationToken);

    private static StudentLessonDto CreateResponse(
        StudentLessonProjection lesson,
        IReadOnlyList<StudentQuestionDto> questions,
        LessonProgressStatus? progress,
        long? previousLessonId,
        long? nextLessonId) =>
        new(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Description,
            lesson.ContentHtml,
            lesson.VideoProvider is not null && lesson.VideoExternalId is not null
                ? new VideoDto(lesson.VideoProvider.Value, lesson.VideoExternalId)
                : null,
            progress.ToContractValue(),
            previousLessonId,
            nextLessonId,
            questions);

    private Task<List<StudentQuestionDto>> LoadQuestionsAsync(
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
                question.Options
                    .OrderBy(option => option.SortOrder)
                    .Select(option => new StudentQuestionOptionDto(option.Id, option.Content))
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
                 (candidate.SortOrder == sortOrder && candidate.Id > lessonId)))
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
        string? VideoExternalId);
}
