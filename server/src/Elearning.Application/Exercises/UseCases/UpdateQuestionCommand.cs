namespace Elearning.Application.Exercises;

public sealed record UpdateQuestionCommand(long QuestionId, QuestionWriteRequest Question);
