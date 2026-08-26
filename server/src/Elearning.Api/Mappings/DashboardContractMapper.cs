using Elearning.Api.Contracts.Dashboard;
using Elearning.Application.Dashboard;

namespace Elearning.Api.Mappings;

public static class DashboardContractMapper
{
    public static DashboardResponse ToResponse(this DashboardDto dashboard) =>
        new(
            dashboard.CourseCount,
            dashboard.StudentCount,
            dashboard.LessonCount,
            dashboard.LessonCompletionCount);
}
