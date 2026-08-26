namespace Elearning.Api.Contracts.Routing;

public static class AdminQuestionRoutes
{
    public const string Controller = "api/admin";
    public const string LessonQuestions = "lessons/{lessonId:long}/questions";
    public const string QuestionById = "questions/{id:long}";

    public static string Location(long questionId) => $"/api/admin/questions/{questionId}";
}
