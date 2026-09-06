using Elearning.Api.Contracts.Common;
using Elearning.Api.Contracts.Students.Responses;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Common;
using Elearning.Application.Students;
using Elearning.Domain;
using Microsoft.AspNetCore.Mvc;
using StudentCreateRequest = Elearning.Api.Contracts.Students.Requests.StudentCreateRequest;
using StudentUpdateRequest = Elearning.Api.Contracts.Students.Requests.StudentUpdateRequest;
using Elearning.Api.Contracts.Routing;

namespace Elearning.Api.Controllers;

[ApiController]
[RequireRole(UserRole.Admin)]
[Route(AdminStudentRoutes.Controller)]
public sealed class AdminStudentsController(
    IAdminStudentQueryHandler studentQueries,
    IStudentAccountCommandHandler accountCommands) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CursorPageResponse<StudentListItemResponse>>> GetStudents(
        int limit = RequestValidation.DefaultLimit,
        string? cursor = null,
        string? search = null,
        AccountStatus? status = null,
        CancellationToken cancellationToken = default) =>
        Ok((await studentQueries.ExecuteAsync(
            new ListStudentsQuery(limit, cursor, search, status),
            cancellationToken)).ToResponse());

    [HttpGet(AdminStudentRoutes.ById)]
    public async Task<ActionResult<StudentDetailResponse>> GetStudent(
        long id,
        CancellationToken cancellationToken) =>
        Ok((await studentQueries.ExecuteAsync(
            new GetStudentQuery(id),
            cancellationToken)).ToResponse());

    [HttpPost]
    public async Task<ActionResult<StudentDetailResponse>> CreateStudent(
        StudentCreateRequest request,
        CancellationToken cancellationToken)
    {
        var student = await accountCommands.ExecuteAsync(
            new CreateStudentCommand(request.ToApplication()),
            cancellationToken);
        return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student.ToResponse());
    }

    [HttpPut(AdminStudentRoutes.ById)]
    public async Task<ActionResult<StudentDetailResponse>> UpdateStudent(
        long id,
        StudentUpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok((await accountCommands.ExecuteAsync(
            new UpdateStudentCommand(id, request.ToApplication()),
            cancellationToken)).ToResponse());

    [HttpPost(AdminStudentRoutes.Disable)]
    public async Task<IActionResult> DisableStudent(long id, CancellationToken cancellationToken)
    {
        await accountCommands.ExecuteAsync(
            new DisableStudentCommand(id, User.GetRequiredUserId()),
            cancellationToken);
        return NoContent();
    }
}
