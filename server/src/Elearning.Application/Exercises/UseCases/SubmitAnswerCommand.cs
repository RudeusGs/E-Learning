namespace Elearning.Application.Exercises;

public sealed record SubmitAnswerCommand(long StudentId, long QuestionId, AnswerRequest Answer);
