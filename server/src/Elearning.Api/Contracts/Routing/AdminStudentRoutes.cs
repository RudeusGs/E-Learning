namespace Elearning.Api.Contracts.Routing;

public static class AdminStudentRoutes
{
    public const string Controller = "api/admin/students";
    public const string ById = "{id:long}";
    public const string Disable = "{id:long}/disable";
}
