namespace Elearning.Application.Exercises;

public sealed record AnswerResultDto(bool Correct, string? Explanation, DateTimeOffset AnsweredAtUtc);
