namespace Elearning.Api.Contracts.Exercises.Responses;

public sealed record AdminQuestionOptionResponse(
    long Id,
    string Content,
    bool IsCorrect,
    int SortOrder);
