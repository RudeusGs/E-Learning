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

    public async Task<CompleteLessonDto> ExecuteAsync(
        CompleteLessonCommand command,
        CancellationToken cancellationToken)
    {
        var studentId = command.StudentId;
        var lessonId = command.LessonId;
        var access = await accessPolicy.AuthorizeAsync(studentId, lessonId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var progress = await dbContext.LessonProgress.SingleOrDefaultAsync(
            item => item.StudentId == studentId && item.LessonId == lessonId,
            cancellationToken);
        if (progress is null)
        {
            progress = LessonProgress.CompleteWithoutStart(studentId, lessonId, now);
            dbContext.LessonProgress.Add(progress);
        }
        else
        {
            progress.Complete(now);
        }

        progress = await SaveWithRaceRecoveryAsync(progress, studentId, lessonId, now, cancellationToken);
        var courseProgress = await CalculateCourseProgressAsync(studentId, access.CourseId, cancellationToken);
        return new CompleteLessonDto(
            progress.Status.ToString().ToUpperInvariant(),
            progress.CompletedAtUtc!.Value,
            courseProgress);
    }

    private async Task<LessonProgress> SaveWithRaceRecoveryAsync(
        LessonProgress progress,
        long studentId,
        long lessonId,
        DateTimeOffset now,
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
            var winner = await LoadAsync(studentId, lessonId, cancellationToken);
            if (winner.Status == LessonProgressStatus.Completed)
            {
                return winner;
            }

            winner.Complete(now);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return winner;
            }
            catch (DbUpdateConcurrencyException)
            {
                dbContext.ChangeTracker.Clear();
                return await LoadAsync(studentId, lessonId, cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return await LoadAsync(studentId, lessonId, cancellationToken);
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

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
