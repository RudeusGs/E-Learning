using Elearning.Api.Contracts.Common;
using Elearning.Api.Contracts.Courses.Responses;
using Elearning.Api.Contracts.Routing;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Common;
using Elearning.Application.Courses;
using Elearning.Domain;
using Microsoft.AspNetCore.Mvc;
using CourseWriteRequest = Elearning.Api.Contracts.Courses.Requests.CourseWriteRequest;

namespace Elearning.Api.Controllers;

[ApiController]
[RequireRole(UserRole.Admin)]
[Route(AdminCourseRoutes.Controller)]
public sealed class AdminCoursesController(
    IAdminCourseQueryHandler courseQueries,
    IAdminCourseCommandHandler courseCommands) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CursorPageResponse<CourseResponse>>> GetCourses(
        int limit = RequestValidation.DefaultLimit,
        string? cursor = null,
        string? search = null,
        CourseStatus? status = null,
        string? sort = null,
        CancellationToken cancellationToken = default) =>
        Ok((await courseQueries.ExecuteAsync(
            new ListAdminCoursesQuery(limit, cursor, search, status, sort),
            cancellationToken)).ToResponse());

    [HttpGet(AdminCourseRoutes.ById)]
    public async Task<ActionResult<CourseResponse>> GetCourse(long id, CancellationToken cancellationToken) =>
        Ok((await courseQueries.ExecuteAsync(new GetAdminCourseQuery(id), cancellationToken)).ToResponse());

    [HttpPost]
    public async Task<ActionResult<CourseResponse>> CreateCourse(
        CourseWriteRequest request,
        CancellationToken cancellationToken)
    {
        var course = await courseCommands.ExecuteAsync(
            new CreateCourseCommand(request.ToApplication()),
            cancellationToken);
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course.ToResponse());
    }

    [HttpPut(AdminCourseRoutes.ById)]
    public async Task<ActionResult<CourseResponse>> UpdateCourse(
        long id,
        CourseWriteRequest request,
        CancellationToken cancellationToken) =>
        Ok((await courseCommands.ExecuteAsync(
            new UpdateCourseCommand(id, request.ToApplication()),
            cancellationToken)).ToResponse());

    [HttpDelete(AdminCourseRoutes.ById)]
    public async Task<IActionResult> ArchiveCourse(long id, CancellationToken cancellationToken)
    {
        await courseCommands.ExecuteAsync(new ArchiveCourseCommand(id), cancellationToken);
        return NoContent();
    }
}
