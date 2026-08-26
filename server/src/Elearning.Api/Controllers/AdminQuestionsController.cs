using Elearning.Api.Contracts.Exercises.Responses;
using Elearning.Api.Contracts.Routing;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Exercises;
using Elearning.Domain;
using Microsoft.AspNetCore.Mvc;
using QuestionWriteRequest = Elearning.Api.Contracts.Exercises.Requests.QuestionWriteRequest;

namespace Elearning.Api.Controllers;

[ApiController]
[RequireRole(UserRole.Admin)]
[Route(AdminQuestionRoutes.Controller)]
public sealed class AdminQuestionsController(
    IAdminQuestionQueryHandler questionQueries,
    IAdminQuestionCommandHandler questionCommands) : ControllerBase
{
    [HttpGet(AdminQuestionRoutes.LessonQuestions)]
    public async Task<ActionResult<IReadOnlyList<AdminQuestionResponse>>> GetQuestions(
        long lessonId,
        CancellationToken cancellationToken) =>
        Ok((await questionQueries.ExecuteAsync(
            new ListAdminQuestionsQuery(lessonId),
            cancellationToken)).ToResponse());

    [HttpPost(AdminQuestionRoutes.LessonQuestions)]
    public async Task<ActionResult<AdminQuestionResponse>> CreateQuestion(
        long lessonId,
        QuestionWriteRequest request,
        CancellationToken cancellationToken)
    {
        var question = await questionCommands.ExecuteAsync(
            new CreateQuestionCommand(lessonId, request.ToApplication()),
            cancellationToken);
        return Created(AdminQuestionRoutes.Location(question.Id), question.ToResponse());
    }

    [HttpPut(AdminQuestionRoutes.QuestionById)]
    public async Task<ActionResult<AdminQuestionResponse>> UpdateQuestion(
        long id,
        QuestionWriteRequest request,
        CancellationToken cancellationToken) =>
        Ok((await questionCommands.ExecuteAsync(
            new UpdateQuestionCommand(id, request.ToApplication()),
            cancellationToken)).ToResponse());

    [HttpDelete(AdminQuestionRoutes.QuestionById)]
    public async Task<IActionResult> DeleteQuestion(long id, CancellationToken cancellationToken)
    {
        await questionCommands.ExecuteAsync(new DeleteQuestionCommand(id), cancellationToken);
        return NoContent();
    }
}
