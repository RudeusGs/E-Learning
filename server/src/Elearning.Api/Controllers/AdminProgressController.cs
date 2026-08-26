using Elearning.Api.Contracts.Common;
using Elearning.Api.Contracts.Progress.Responses;
using Elearning.Api.Contracts.Routing;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Common;
using Elearning.Application.Progress;
using Elearning.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Api.Controllers;

[ApiController]
[RequireRole(UserRole.Admin)]
[Route(AdminProgressRoutes.Controller)]
public sealed class AdminProgressController(
    IAdminProgressQueryHandler progressQuery) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CursorPageResponse<ProgressRowResponse>>> GetProgress(
        int limit = RequestValidation.DefaultLimit,
        string? cursor = null,
        long? studentId = null,
        long? courseId = null,
        string? search = null,
        CancellationToken cancellationToken = default) =>
        Ok((await progressQuery.ExecuteAsync(
            new GetAdminProgressQuery(limit, cursor, studentId, courseId, search),
            cancellationToken)).ToResponse());
}
