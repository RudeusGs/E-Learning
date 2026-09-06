using Elearning.Api.Contracts.Lessons.Requests;
using Elearning.Api.Contracts.Lessons.Responses;
using Elearning.Api.Contracts.Progress.Responses;
using Elearning.Api.Contracts.Routing;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Lessons;
using Elearning.Application.Progress;
using Elearning.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elearning.Api.Controllers;

[ApiController]
[RequireRole(UserRole.Student)]
[Route(StudentLessonRoutes.Controller)]
public sealed class StudentLessonsController(
    IStudentLessonQueryHandler lessonQuery,
    ILessonProgressCommandHandler progressCommands) : ControllerBase
{
    [HttpGet(StudentLessonRoutes.ById)]
    public async Task<ActionResult<StudentLessonResponse>> GetLesson(
        long lessonId,
        CancellationToken cancellationToken) =>
        Ok((await lessonQuery.ExecuteAsync(
            new GetStudentLessonQuery(User.GetRequiredUserId(), lessonId),
            cancellationToken)).ToResponse());

    [HttpPost(StudentLessonRoutes.Start)]
    public async Task<ActionResult<LessonProgressResponse>> StartLesson(
        long lessonId,
        CancellationToken cancellationToken) =>
        Ok((await progressCommands.ExecuteAsync(
            new StartLessonCommand(User.GetRequiredUserId(), lessonId),
            cancellationToken)).ToResponse());

    [EnableRateLimiting(RateLimitPolicyNames.StudentInteraction)]
    [HttpPost(StudentLessonRoutes.VideoHeartbeat)]
    public async Task<ActionResult<VideoProgressResponse>> RecordVideoHeartbeat(
        long lessonId,
        VideoHeartbeatRequest request,
        CancellationToken cancellationToken) =>
        Ok((await progressCommands.ExecuteAsync(
            new RecordVideoHeartbeatCommand(
                User.GetRequiredUserId(),
                lessonId,
                request.PositionSeconds),
            cancellationToken)).ToResponse());

    [HttpPost(StudentLessonRoutes.Complete)]
    public async Task<ActionResult<CompleteLessonResponse>> CompleteLesson(
        long lessonId,
        CancellationToken cancellationToken) =>
        Ok((await progressCommands.ExecuteAsync(
            new CompleteLessonCommand(User.GetRequiredUserId(), lessonId),
            cancellationToken)).ToResponse());
}