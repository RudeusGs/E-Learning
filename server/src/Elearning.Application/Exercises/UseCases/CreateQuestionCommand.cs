namespace Elearning.Application.Exercises;

public sealed record CreateQuestionCommand(long LessonId, QuestionWriteRequest Question);
