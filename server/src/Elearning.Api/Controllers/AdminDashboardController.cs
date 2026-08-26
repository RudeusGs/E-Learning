using Elearning.Api.Contracts.Dashboard;
using Elearning.Api.Contracts.Routing;
using Elearning.Api.Mappings;
using Elearning.Api.Security;
using Elearning.Application.Dashboard;
using Elearning.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Api.Controllers;

[ApiController]
[RequireRole(UserRole.Admin)]
[Route(AdminDashboardRoutes.Controller)]
public sealed class AdminDashboardController(
    IAdminDashboardQueryHandler dashboardQuery) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken) =>
        Ok((await dashboardQuery.ExecuteAsync(new GetAdminDashboardQuery(), cancellationToken)).ToResponse());
}
