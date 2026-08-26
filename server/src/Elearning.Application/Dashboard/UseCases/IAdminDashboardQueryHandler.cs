namespace Elearning.Application.Dashboard;

public interface IAdminDashboardQueryHandler
{
    Task<DashboardDto> ExecuteAsync(
        GetAdminDashboardQuery query,
        CancellationToken cancellationToken);
}
