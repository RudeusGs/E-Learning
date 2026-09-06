
using Elearning.Domain;

namespace Elearning.Api.Contracts.Exercises.Responses;

public sealed record StudentQuestionResponse(
    long Id,
    string Text,
    QuestionType Type,
    QuestionPlacement Placement,
    int? VideoTimestampSeconds,
    bool Passed,
    IReadOnlyList<StudentQuestionOptionResponse> Options);
