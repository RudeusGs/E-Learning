using Elearning.Api.Contracts.Common;
using Elearning.Api.Contracts.Courses.Responses;
using Elearning.Api.Contracts.Routing;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Common;
using Elearning.Application.Courses;
using Elearning.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Api.Controllers;

[ApiController]
[RequireRole(UserRole.Student)]
[Route(StudentCourseRoutes.Controller)]
public sealed class StudentCoursesController(
    IStudentCourseCatalogQueryHandler courseCatalog,
    IStudentCourseDetailQueryHandler courseDetails) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CursorPageResponse<StudentCourseResponse>>> GetCourses(
        int limit = RequestValidation.DefaultLimit,
        string? cursor = null,
        string? progress = null,
        CancellationToken cancellationToken = default) =>
        Ok((await courseCatalog.ExecuteAsync(
            new ListStudentCoursesQuery(User.GetRequiredUserId(), limit, cursor, progress),
            cancellationToken)).ToResponse());

    [HttpGet(StudentCourseRoutes.ById)]
    public async Task<ActionResult<StudentCourseDetailResponse>> GetCourse(
        long courseId,
        CancellationToken cancellationToken) =>
        Ok((await courseDetails.ExecuteAsync(
            new GetStudentCourseQuery(User.GetRequiredUserId(), courseId),
            cancellationToken)).ToResponse());
}
