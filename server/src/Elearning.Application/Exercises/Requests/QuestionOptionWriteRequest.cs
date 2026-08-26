namespace Elearning.Application.Exercises;

public sealed record QuestionOptionWriteRequest(string Content, bool IsCorrect, int SortOrder);
