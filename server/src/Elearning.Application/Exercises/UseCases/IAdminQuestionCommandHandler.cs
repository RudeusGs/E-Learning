namespace Elearning.Application.Exercises;

public interface IAdminQuestionCommandHandler
{
    Task<AdminQuestionDto> ExecuteAsync(CreateQuestionCommand command, CancellationToken cancellationToken);
    Task<AdminQuestionDto> ExecuteAsync(UpdateQuestionCommand command, CancellationToken cancellationToken);
    Task ExecuteAsync(DeleteQuestionCommand command, CancellationToken cancellationToken);
}
