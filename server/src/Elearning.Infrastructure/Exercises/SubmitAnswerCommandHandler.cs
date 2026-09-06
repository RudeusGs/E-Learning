using Elearning.Application.Errors;
using Elearning.Application.Exceptions;
using Elearning.Application.Exercises;
using Elearning.Domain;
using Elearning.Infrastructure.Authorization;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Exercises;

public sealed class SubmitAnswerCommandHandler(
    ElearningDbContext dbContext,
    StudentLessonAccessPolicy accessPolicy,
    TimeProvider timeProvider) : ISubmitAnswerCommandHandler
{
    public async Task<AnswerResultDto> ExecuteAsync(
        SubmitAnswerCommand command,
        CancellationToken cancellationToken)
    {
        var studentId = command.StudentId;
        var questionId = command.QuestionId;
        var request = command.Answer;

        var question = await dbContext.Questions
            .AsNoTracking()
            .Where(candidate => candidate.Id == questionId)
            .Select(candidate => new
            {
                candidate.LessonId,
                candidate.Explanation,
                candidate.Placement,
                candidate.VideoTimestampSeconds,
                HasVideo = candidate.Lesson.VideoProvider != null
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException("Question");

        await accessPolicy.AuthorizeAsync(studentId, question.LessonId, cancellationToken);

        var progress = await dbContext.LessonProgress
            .AsNoTracking()
            .Where(item => item.StudentId == studentId && item.LessonId == question.LessonId)
            .Select(item => new
            {
                item.VideoMaxPositionSeconds,
                item.VideoCompletedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (progress is null)
        {
            throw new ConflictException(
                ErrorCodes.LessonNotStarted,
                "Lesson has not been started",
                "Open the lesson before answering its questions.");
        }

        if (question.Placement == QuestionPlacement.VideoCheckpoint)
        {
            var checkpoint = question.VideoTimestampSeconds ?? int.MaxValue;
            if (progress.VideoMaxPositionSeconds < Math.Max(0, checkpoint - 2))
            {
                throw new ConflictException(
                    ErrorCodes.QuestionNotAvailable,
                    "The checkpoint is not available yet",
                    "Continue watching the video until this checkpoint appears.");
            }
        }
        else if (question.HasVideo && progress.VideoCompletedAtUtc is null)
        {
            throw new ConflictException(
                ErrorCodes.VideoNotCompleted,
                "The reinforcement exercise is locked",
                "Watch the video to the end before answering reinforcement questions.");
        }

        var option = await dbContext.QuestionOptions
            .AsNoTracking()
            .Where(candidate => candidate.Id == request.OptionId && candidate.QuestionId == questionId)
            .Select(candidate => new { candidate.Id, candidate.IsCorrect })
            .SingleOrDefaultAsync(cancellationToken);

        if (option is null)
        {
            throw new RequestValidationException(
                ErrorCodes.InvalidQuestionOption,
                "The selected option does not belong to this question");
        }

        var answeredAt = timeProvider.GetUtcNow();
        dbContext.StudentAnswers.Add(StudentAnswer.Create(
            studentId,
            questionId,
            option.Id,
            option.IsCorrect,
            answeredAt));
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AnswerResultDto(option.IsCorrect, question.Explanation, answeredAt);
    }
}
