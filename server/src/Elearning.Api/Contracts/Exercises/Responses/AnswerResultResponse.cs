namespace Elearning.Api.Contracts.Exercises.Responses;

public sealed record AnswerResultResponse(
    bool Correct,
    string? Explanation,
    DateTimeOffset AnsweredAtUtc);
