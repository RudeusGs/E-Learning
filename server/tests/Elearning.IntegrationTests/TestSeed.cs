namespace Elearning.IntegrationTests;

public sealed record TestSeed(
    long AdminId,
    long StudentAId,
    long StudentBId,
    long DisabledStudentId,
    long CourseAId,
    long CourseBId,
    long LessonA1Id,
    long LessonA2Id,
    long LessonB1Id,
    long QuestionAId,
    long QuestionBId,
    long QuestionACorrectOptionId,
    long QuestionBCorrectOptionId);
