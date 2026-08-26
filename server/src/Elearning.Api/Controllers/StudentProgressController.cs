using Elearning.Api.Contracts.Progress.Responses;
using Elearning.Api.Contracts.Routing;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Progress;
using Elearning.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Api.Controllers;

[ApiController]
[RequireRole(UserRole.Student)]
[Route(StudentProgressRoutes.Controller)]
public sealed class StudentProgressController(
    IStudentProgressQueryHandler progressQuery) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StudentCourseProgressResponse>> GetProgress(
        long courseId,
        CancellationToken cancellationToken) =>
        Ok((await progressQuery.ExecuteAsync(
            new GetStudentProgressQuery(User.GetRequiredUserId(), courseId),
            cancellationToken)).ToResponse());
}
