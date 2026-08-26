namespace Elearning.Api.Contracts.Routing;

public static class StudentExerciseRoutes
{
    public const string Controller = "api/student/questions";
    public const string SubmitAnswer = "{questionId:long}/answer";
}
