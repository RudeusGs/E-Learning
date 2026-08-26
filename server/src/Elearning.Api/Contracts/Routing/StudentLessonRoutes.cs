namespace Elearning.Api.Contracts.Routing;

public static class StudentLessonRoutes
{
    public const string Controller = "api/student/lessons";
    public const string ById = "{lessonId:long}";
    public const string Start = "{lessonId:long}/start";
    public const string Complete = "{lessonId:long}/complete";
}
