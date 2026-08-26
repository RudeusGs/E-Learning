using Elearning.Domain;

namespace Elearning.Application.Exercises;

public sealed record QuestionWriteRequest(
    string Text,
    QuestionType Type,
    string? Explanation,
    int SortOrder,
    IReadOnlyList<QuestionOptionWriteRequest> Options,
    long? Version = null);
