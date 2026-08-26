using Elearning.Api.Contracts.Lessons.Responses;
using Elearning.Api.Contracts.Routing;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Lessons;
using Elearning.Domain;
using Microsoft.AspNetCore.Mvc;
using LessonWriteRequest = Elearning.Api.Contracts.Lessons.Requests.LessonWriteRequest;

namespace Elearning.Api.Controllers;

[ApiController]
[RequireRole(UserRole.Admin)]
[Route(AdminLessonRoutes.Controller)]
public sealed class AdminLessonsController(
    IAdminLessonQueryHandler lessonQueries,
    IAdminLessonCommandHandler lessonCommands) : ControllerBase
{
    [HttpGet(AdminLessonRoutes.CourseLessons)]
    public async Task<ActionResult<IReadOnlyList<LessonAdminResponse>>> GetLessons(
        long courseId,
        CancellationToken cancellationToken) =>
        Ok((await lessonQueries.ExecuteAsync(
            new ListAdminLessonsQuery(courseId),
            cancellationToken)).ToResponse());

    [HttpGet(AdminLessonRoutes.LessonById)]
    public async Task<ActionResult<LessonAdminResponse>> GetLesson(long id, CancellationToken cancellationToken) =>
        Ok((await lessonQueries.ExecuteAsync(new GetAdminLessonQuery(id), cancellationToken)).ToResponse());

    [HttpPost(AdminLessonRoutes.CourseLessons)]
    public async Task<ActionResult<LessonAdminResponse>> CreateLesson(
        long courseId,
        LessonWriteRequest request,
        CancellationToken cancellationToken)
    {
        var lesson = await lessonCommands.ExecuteAsync(
            new CreateLessonCommand(courseId, request.ToApplication()),
            cancellationToken);
        return CreatedAtAction(nameof(GetLesson), new { id = lesson.Id }, lesson.ToResponse());
    }

    [HttpPut(AdminLessonRoutes.LessonById)]
    public async Task<ActionResult<LessonAdminResponse>> UpdateLesson(
        long id,
        LessonWriteRequest request,
        CancellationToken cancellationToken) =>
        Ok((await lessonCommands.ExecuteAsync(
            new UpdateLessonCommand(id, request.ToApplication()),
            cancellationToken)).ToResponse());

    [HttpDelete(AdminLessonRoutes.LessonById)]
    public async Task<IActionResult> ArchiveLesson(long id, CancellationToken cancellationToken)
    {
        await lessonCommands.ExecuteAsync(new ArchiveLessonCommand(id), cancellationToken);
        return NoContent();
    }
}
