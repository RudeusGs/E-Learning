using Elearning.Application.Errors;
using Elearning.Application.Exceptions;
using Elearning.Application.Lessons;
using Elearning.Application.Progress;
using Elearning.Domain;
using Elearning.Infrastructure.Authorization;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Elearning.Infrastructure.Lessons;

public sealed class LessonProgressCommandHandler(
    ElearningDbContext dbContext,
    StudentLessonAccessPolicy accessPolicy,
    LessonCompletionStateCalculator completionCalculator,
    TimeProvider timeProvider) : ILessonProgressCommandHandler
{
    public async Task<LessonProgressDto> ExecuteAsync(
        StartLessonCommand command,
        CancellationToken cancellationToken)
    {
        var studentId = command.StudentId;
        var lessonId = command.LessonId;
        await accessPolicy.AuthorizeAsync(studentId, lessonId, cancellationToken);

        var progress = await dbContext.LessonProgress.SingleOrDefaultAsync(
            item => item.StudentId == studentId && item.LessonId == lessonId,
            cancellationToken);

        if (progress is null)
        {
            progress = LessonProgress.Start(studentId, lessonId, timeProvider.GetUtcNow());
            dbContext.LessonProgress.Add(progress);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                dbContext.ChangeTracker.Clear();
                progress = await LoadAsync(studentId, lessonId, cancellationToken);
            }
        }
        return LessonProgressMapper.ToDto(progress);
    }

    public async Task<VideoProgressDto> ExecuteAsync(
        RecordVideoHeartbeatCommand command,
        CancellationToken cancellationToken)
    {
        var studentId = command.StudentId;
        var lessonId = command.LessonId;
        await accessPolicy.AuthorizeAsync(studentId, lessonId, cancellationToken);

        var lesson = await dbContext.Lessons
            .AsNoTracking()
            .Where(item => item.Id == lessonId)
            .Select(item => new
            {
                HasVideo = item.VideoProvider != null,
                item.VideoDurationSeconds
            })
            .SingleAsync(cancellationToken);

        if (!lesson.HasVideo || lesson.VideoDurationSeconds is not > 0)
        {
            throw new ConflictException(
                ErrorCodes.VideoDurationRequired,
                "Video tracking is not configured",
                "The lesson needs a trusted video duration before progress can be tracked.");
        }

        var progress = await dbContext.LessonProgress.SingleOrDefaultAsync(
            item => item.StudentId == studentId && item.LessonId == lessonId,
            cancellationToken);

        if (progress is null)
        {
            progress = LessonProgress.Start(studentId, lessonId, timeProvider.GetUtcNow());
            progress.BeginVideoTracking(timeProvider.GetUtcNow());
            dbContext.LessonProgress.Add(progress);
            progress = await SaveCreateWithRaceRecoveryAsync(progress, studentId, lessonId, cancellationToken);
        }

        var pendingCheckpoint = await dbContext.Questions
            .AsNoTracking()
            .Where(question =>
                question.LessonId == lessonId &&
                question.Placement == QuestionPlacement.VideoCheckpoint &&
                question.VideoTimestampSeconds != null &&
                !dbContext.StudentAnswers.Any(answer =>
                    answer.StudentId == studentId &&
                    answer.QuestionId == question.Id &&
                    answer.IsCorrect))
            .OrderBy(question => question.VideoTimestampSeconds)
            .ThenBy(question => question.Id)
            .Select(question => new
            {
                question.Id,
                Timestamp = question.VideoTimestampSeconds!.Value
            })
            .FirstOrDefaultAsync(cancellationToken);

        var duration = lesson.VideoDurationSeconds.Value;
        var maximumAllowed = pendingCheckpoint?.Timestamp ?? duration;
        var accepted = progress.RecordVideoHeartbeat(
            command.PositionSeconds,
            duration,
            maximumAllowed,
            timeProvider.GetUtcNow());

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            progress = await LoadAsync(studentId, lessonId, cancellationToken);
            accepted = progress.VideoMaxPositionSeconds;
        }

        var blockedQuestionId = pendingCheckpoint is not null &&
            accepted >= Math.Max(0, pendingCheckpoint.Timestamp - LessonProgress.VideoEndToleranceSeconds)
                ? pendingCheckpoint.Id
                : (long?)null;

        return new VideoProgressDto(
            accepted,
            duration,
            progress.VideoCompletedAtUtc is not null,
            blockedQuestionId);
    }

    public async Task<CompleteLessonDto> ExecuteAsync(
        CompleteLessonCommand command,
        CancellationToken cancellationToken)
    {
        var studentId = command.StudentId;
        var lessonId = command.LessonId;
        var access = await accessPolicy.AuthorizeAsync(studentId, lessonId, cancellationToken);

        var lesson = await dbContext.Lessons
            .AsNoTracking()
            .Where(item => item.Id == lessonId)
            .Select(item => new
            {
                VideoRequired = item.VideoProvider != null,
                item.VideoDurationSeconds
            })
            .SingleAsync(cancellationToken);

        var progress = await dbContext.LessonProgress.SingleOrDefaultAsync(
            item => item.StudentId == studentId && item.LessonId == lessonId,
            cancellationToken);

        if (progress is null)
        {
            throw new ConflictException(
                ErrorCodes.LessonNotStarted,
                "Lesson has not been started",
                "Open and study the lesson before attempting to complete it.");
        }

        if (progress.Status != LessonProgressStatus.Completed)
        {
            var completion = await completionCalculator.CalculateAsync(
                studentId,
                lessonId,
                lesson.VideoRequired,
                lesson.VideoDurationSeconds,
                progress.Status,
                progress.VideoCompletedAtUtc,
                cancellationToken);

            LessonCompletionStateCalculator.EnsureCanComplete(completion);
            progress.Complete(timeProvider.GetUtcNow());
            progress = await SaveCompleteWithConcurrencyRecoveryAsync(progress, studentId, lessonId, cancellationToken);
        }

        var courseProgress = await CalculateCourseProgressAsync(studentId, access.CourseId, cancellationToken);
        var nextLessonId = await FindNextLessonIdAsync(access, cancellationToken);

        return new CompleteLessonDto(
            progress.Status.ToString().ToUpperInvariant(),
            progress.CompletedAtUtc!.Value,
            courseProgress,
            nextLessonId);
    }

    private async Task<LessonProgress> SaveCreateWithRaceRecoveryAsync(
        LessonProgress progress,
        long studentId,
        long lessonId,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return progress;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            return await LoadAsync(studentId, lessonId, cancellationToken);
        }
    }

    private async Task<LessonProgress> SaveCompleteWithConcurrencyRecoveryAsync(
        LessonProgress progress,
        long studentId,
        long lessonId,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return progress;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            var latest = await LoadAsync(studentId, lessonId, cancellationToken);
            if (latest.Status != LessonProgressStatus.Completed)
            {
                throw;
            }
            return latest;
        }
    }

    private async Task<CourseProgressSummaryDto> CalculateCourseProgressAsync(
        long studentId,
        long courseId,
        CancellationToken cancellationToken)
    {
        var total = await dbContext.Lessons.AsNoTracking().CountAsync(
            lesson => lesson.CourseId == courseId && lesson.Status == LessonStatus.Published,
            cancellationToken);
        var completed = await dbContext.Lessons.AsNoTracking().CountAsync(
            lesson => lesson.CourseId == courseId && lesson.Status == LessonStatus.Published &&
                lesson.Progress.Any(progress =>
                    progress.StudentId == studentId && progress.Status == LessonProgressStatus.Completed),
            cancellationToken);
        var result = CourseProgress.Calculate(completed, total);
        return new CourseProgressSummaryDto(result.CompletedLessons, result.TotalLessons, result.Percentage);
    }

    private Task<LessonProgress> LoadAsync(
        long studentId,
        long lessonId,
        CancellationToken cancellationToken) =>
        dbContext.LessonProgress.SingleAsync(
            item => item.StudentId == studentId && item.LessonId == lessonId,
            cancellationToken);

    private Task<long?> FindNextLessonIdAsync(
        StudentLessonAccess access,
        CancellationToken cancellationToken) =>
        dbContext.Lessons
            .AsNoTracking()
            .Where(lesson =>
                lesson.CourseId == access.CourseId &&
                lesson.Status == LessonStatus.Published &&
                (lesson.SortOrder > access.SortOrder ||
                 (lesson.SortOrder == access.SortOrder && lesson.Id > access.LessonId)))
            .OrderBy(lesson => lesson.SortOrder)
            .ThenBy(lesson => lesson.Id)
            .Select(lesson => (long?)lesson.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
