namespace Elearning.Application.Exercises;

public interface ISubmitAnswerCommandHandler
{
    Task<AnswerResultDto> ExecuteAsync(SubmitAnswerCommand command, CancellationToken cancellationToken);
}
