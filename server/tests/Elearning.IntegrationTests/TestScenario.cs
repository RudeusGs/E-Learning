namespace Elearning.IntegrationTests;

public sealed record TestScenario(
    string StudentEmail,
    long StudentId,
    long CourseId,
    IReadOnlyList<long> LessonIds,
    long QuestionId,
    long CorrectOptionId,
    long IncorrectOptionId);
