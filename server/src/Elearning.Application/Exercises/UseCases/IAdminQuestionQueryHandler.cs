namespace Elearning.Application.Exercises;

public interface IAdminQuestionQueryHandler
{
    Task<IReadOnlyList<AdminQuestionDto>> ExecuteAsync(
        ListAdminQuestionsQuery query,
        CancellationToken cancellationToken);
}
