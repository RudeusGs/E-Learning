namespace Elearning.Api.Contracts.Exercises.Requests;

public sealed record QuestionOptionWriteRequest(string Content, bool IsCorrect, int SortOrder);
