
using Elearning.Application.Errors;
using Elearning.Application.Exceptions;
using Elearning.Application.Exercises;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Elearning.Infrastructure.Exercises;

public sealed class AdminQuestionCommandHandler(
    ElearningDbContext dbContext,
    TimeProvider timeProvider) : IAdminQuestionCommandHandler
{
    public async Task<AdminQuestionDto> ExecuteAsync(
        CreateQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Question;
        await ValidatePlacementAsync(
            command.LessonId,
            request,
            cancellationToken);

        var question = Question.Create(
            command.LessonId,
            request.Text,
            request.Type,
            request.Explanation,
            request.SortOrder,
            QuestionMapper.ToDrafts(request),
            timeProvider.GetUtcNow(),
            request.Placement,
            request.VideoTimestampSeconds);
        dbContext.Questions.Add(question);
        await SaveWithConflictMappingAsync(cancellationToken);
        return QuestionMapper.ToAdminDto(question);
    }

    public async Task<AdminQuestionDto> ExecuteAsync(
        UpdateQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Question;
        if (request.Version is null)
        {
            throw new RequestValidationException(
                "Version is required",
                "Supply the version returned by the latest read.");
        }

        var question = await dbContext.Questions
            .Include(candidate => candidate.Options)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == command.QuestionId,
                cancellationToken)
            ?? throw new ResourceNotFoundException("Question");

        if (question.Version != request.Version)
        {
            throw new ResourceConcurrencyException("Question");
        }

        if (await HasAnswerHistoryAsync(command.QuestionId, cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.QuestionHasHistory,
                "Question has answer history",
                "Create a replacement question instead of changing a question with historical attempts.");
        }

        await ValidatePlacementAsync(
            question.LessonId,
            request,
            cancellationToken);

        question.Update(
            request.Text,
            request.Type,
            request.Explanation,
            request.SortOrder,
            QuestionMapper.ToDrafts(request),
            timeProvider.GetUtcNow(),
            request.Placement,
            request.VideoTimestampSeconds);
        await SaveWithConflictMappingAsync(cancellationToken);
        return QuestionMapper.ToAdminDto(question);
    }

    public async Task ExecuteAsync(
        DeleteQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var question = await dbContext.Questions
            .Include(candidate => candidate.Options)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == command.QuestionId,
                cancellationToken)
            ?? throw new ResourceNotFoundException("Question");

        if (await HasAnswerHistoryAsync(command.QuestionId, cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.QuestionHasHistory,
                "Question with answer history cannot be deleted");
        }

        dbContext.QuestionOptions.RemoveRange(question.Options);
        dbContext.Questions.Remove(question);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidatePlacementAsync(
        long lessonId,
        QuestionWriteRequest request,
        CancellationToken cancellationToken)
    {
        var lesson = await dbContext.Lessons
            .AsNoTracking()
            .Where(candidate => candidate.Id == lessonId)
            .Select(candidate => new
            {
                candidate.VideoProvider,
                candidate.VideoDurationSeconds
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException("Lesson");

        if (request.Placement != QuestionPlacement.VideoCheckpoint)
        {
            return;
        }

        if (lesson.VideoProvider is null)
        {
            throw new RequestValidationException(
                "Checkpoint question requires a video",
                "Choose reinforcement placement or add a video to the lesson.");
        }

        if (lesson.VideoDurationSeconds is null)
        {
            throw new RequestValidationException(
                ErrorCodes.VideoDurationRequired,
                "Video duration is required",
                "Configure the lesson video duration before adding in-video checkpoints.");
        }

        if (
            request.VideoTimestampSeconds is null ||
            request.VideoTimestampSeconds < 1 ||
            request.VideoTimestampSeconds >= lesson.VideoDurationSeconds)
        {
            throw new RequestValidationException(
                "Invalid checkpoint timestamp",
                $"Checkpoint time must be between 1 and {lesson.VideoDurationSeconds.Value - 1} seconds.");
        }
    }

    private Task<bool> HasAnswerHistoryAsync(
        long questionId,
        CancellationToken cancellationToken) =>
        dbContext.StudentAnswers.AsNoTracking().AnyAsync(
            answer => answer.QuestionId == questionId,
            cancellationToken);

    private async Task SaveWithConflictMappingAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new ConflictException(
                ErrorCodes.QuestionOrderConflict,
                "Question or option ordering conflicts");
        }
    }
}
