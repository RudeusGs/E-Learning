namespace Elearning.Application.Exercises;

public sealed record AdminQuestionOptionDto(long Id, string Content, bool IsCorrect, int SortOrder);
