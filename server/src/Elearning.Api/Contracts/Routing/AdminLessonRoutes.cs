namespace Elearning.Api.Contracts.Routing;

public static class AdminLessonRoutes
{
    public const string Controller = "api/admin";
    public const string CourseLessons = "courses/{courseId:long}/lessons";
    public const string LessonById = "lessons/{id:long}";
}
