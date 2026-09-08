using Elearning.Application.Errors;
using Elearning.Application.Exceptions;
using Elearning.Application.Lessons;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Lessons;

public sealed class LessonCompletionStateCalculator(
    ElearningDbContext dbContext)
{
    public async Task<LessonCompletionStateDto> CalculateAsync(
        long studentId,
        long lessonId,
        bool videoRequired,
        int? videoDurationSeconds,
        LessonProgressStatus? progressStatus,
        DateTimeOffset? videoCompletedAtUtc,
        CancellationToken cancellationToken)
    {
        var stats = await dbContext.Questions
            .AsNoTracking()
            .Where(question => question.LessonId == lessonId)
            .GroupBy(_ => 1)
            .Select(group => new CompletionQuestionStats(
                group.Count(question =>
                    question.Placement == QuestionPlacement.VideoCheckpoint),
                group.Count(question =>
                    question.Placement == QuestionPlacement.VideoCheckpoint &&
                    dbContext.StudentAnswers.Any(answer =>
                        answer.StudentId == studentId &&
                        answer.QuestionId == question.Id &&
                        answer.IsCorrect)),
                group.Count(question =>
                    question.Placement == QuestionPlacement.Reinforcement),
                group.Count(question =>
                    question.Placement == QuestionPlacement.Reinforcement &&
                    dbContext.StudentAnswers
                        .Where(answer =>
                            answer.StudentId == studentId &&
                            answer.QuestionId == question.Id)
                        .OrderByDescending(answer => answer.AnsweredAtUtc)
                        .ThenByDescending(answer => answer.Id)
                        .Select(answer => answer.IsCorrect)
                        .FirstOrDefault())))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new CompletionQuestionStats(0, 0, 0, 0);

        return CalculateCore(stats, videoRequired, videoDurationSeconds, progressStatus, videoCompletedAtUtc);
    }

    public static LessonCompletionStateDto Calculate(
        IReadOnlyList<Elearning.Application.Exercises.StudentQuestionDto> questions,
        bool videoRequired,
        int? videoDurationSeconds,
        LessonProgressStatus? progressStatus,
        DateTimeOffset? videoCompletedAtUtc)
    {
        var stats = new CompletionQuestionStats(
            questions.Count(q => q.Placement == QuestionPlacement.VideoCheckpoint),
            questions.Count(q => q.Placement == QuestionPlacement.VideoCheckpoint && q.Passed),
            questions.Count(q => q.Placement == QuestionPlacement.Reinforcement),
            questions.Count(q => q.Placement == QuestionPlacement.Reinforcement && q.Passed));

        return CalculateCore(stats, videoRequired, videoDurationSeconds, progressStatus, videoCompletedAtUtc);
    }

    private static LessonCompletionStateDto CalculateCore(
        CompletionQuestionStats stats,
        bool videoRequired,
        int? videoDurationSeconds,
        LessonProgressStatus? progressStatus,
        DateTimeOffset? videoCompletedAtUtc)
    {

        var reinforcementScore = stats.ReinforcementTotal == 0
            ? 100
            : (int)Math.Round(
                stats.ReinforcementPassed * 100d / stats.ReinforcementTotal,
                MidpointRounding.AwayFromZero);

        var alreadyCompleted = progressStatus == LessonProgressStatus.Completed;
        var videoConfigured = !videoRequired || videoDurationSeconds is > 0;
        var videoCompleted = !videoRequired || videoCompletedAtUtc is not null;
        var reinforcementUnlocked = !videoRequired || videoCompleted;
        var checkpointsPassed = stats.CheckpointPassed == stats.CheckpointTotal;
        var quizPassed = LessonCompletionPolicy.IsReinforcementScorePassing(
            stats.ReinforcementPassed,
            stats.ReinforcementTotal);

        var canComplete =
            alreadyCompleted ||
            (videoConfigured &&
             videoCompleted &&
             checkpointsPassed &&
             reinforcementUnlocked &&
             quizPassed);

        return new LessonCompletionStateDto(
            videoRequired,
            videoConfigured,
            videoCompleted,
            reinforcementUnlocked,
            stats.CheckpointTotal,
            stats.CheckpointPassed,
            stats.ReinforcementTotal,
            stats.ReinforcementPassed,
            reinforcementScore,
            LessonCompletionPolicy.RequiredQuizScorePercent,
            canComplete);
    }

    public static void EnsureCanComplete(
        LessonCompletionStateDto state)
    {
        if (!state.VideoConfigured)
        {
            throw new ConflictException(
                ErrorCodes.VideoDurationRequired,
                "Video duration is not configured",
                "An administrator must configure the trusted video duration before this lesson can be completed.");
        }

        if (!state.VideoCompleted)
        {
            throw new ConflictException(
                ErrorCodes.VideoNotCompleted,
                "Video must be watched before completing the lesson",
                "Watch the video to the end without skipping forward.");
        }

        if (state.CheckpointPassed < state.CheckpointTotal)
        {
            throw new ConflictException(
                ErrorCodes.CheckpointRequired,
                "In-video checkpoint is incomplete",
                "Answer every in-video checkpoint correctly before completing the lesson.");
        }

        if (!LessonCompletionPolicy.IsReinforcementScorePassing(
                state.ReinforcementPassed,
                state.ReinforcementTotal,
                state.RequiredScorePercent))
        {
            throw new ConflictException(
                ErrorCodes.QuizNotPassed,
                "Reinforcement score is below the required threshold",
                $"Reach more than {state.RequiredScorePercent}% correct answers before completing the lesson.");
        }
    }

    private sealed record CompletionQuestionStats(
        int CheckpointTotal,
        int CheckpointPassed,
        int ReinforcementTotal,
        int ReinforcementPassed);
}
