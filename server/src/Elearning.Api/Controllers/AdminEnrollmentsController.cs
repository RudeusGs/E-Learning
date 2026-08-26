using Elearning.Api.Contracts.Enrollments.Responses;
using Elearning.Api.Contracts.Routing;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Enrollments;
using Elearning.Application.Students;
using Elearning.Domain;
using Microsoft.AspNetCore.Mvc;
using EnrollmentRequest = Elearning.Api.Contracts.Enrollments.Requests.EnrollmentRequest;

namespace Elearning.Api.Controllers;

[ApiController]
[RequireRole(UserRole.Admin)]
[Route(AdminEnrollmentRoutes.Controller)]
public sealed class AdminEnrollmentsController(
    IEnrollmentCommandHandler enrollmentCommands) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<EnrollmentResponse>> Enroll(
        EnrollmentRequest request,
        CancellationToken cancellationToken) =>
        Ok((await enrollmentCommands.ExecuteAsync(
            new EnrollStudentCommand(request.ToApplication()),
            cancellationToken)).ToResponse());
}
