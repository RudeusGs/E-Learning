
using Elearning.Domain;

namespace Elearning.Api.Contracts.Exercises.Requests;

public sealed record QuestionWriteRequest(
    string Text,
    QuestionType Type,
    string? Explanation,
    int SortOrder,
    IReadOnlyList<QuestionOptionWriteRequest> Options,
    QuestionPlacement Placement = QuestionPlacement.Reinforcement,
    int? VideoTimestampSeconds = null,
    long? Version = null);
