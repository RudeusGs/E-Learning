using Elearning.Api.Contracts.Exercises.Responses;
using Elearning.Api.Contracts.Routing;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Exercises;
using Elearning.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AnswerRequest = Elearning.Api.Contracts.Exercises.Requests.AnswerRequest;

namespace Elearning.Api.Controllers;

[ApiController]
[RequireRole(UserRole.Student)]
[Route(StudentExerciseRoutes.Controller)]
public sealed class StudentExercisesController(
    ISubmitAnswerCommandHandler submitAnswer) : ControllerBase
{
    [EnableRateLimiting(RateLimitPolicyNames.StudentInteraction)]
    [HttpPost(StudentExerciseRoutes.SubmitAnswer)]
    public async Task<ActionResult<AnswerResultResponse>> SubmitAnswer(
        long questionId,
        AnswerRequest request,
        CancellationToken cancellationToken) =>
        Ok((await submitAnswer.ExecuteAsync(
            new SubmitAnswerCommand(
                User.GetRequiredUserId(),
                questionId,
                request.ToApplication()),
            cancellationToken)).ToResponse());
}
