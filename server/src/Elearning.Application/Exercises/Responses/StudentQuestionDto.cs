
using Elearning.Domain;

namespace Elearning.Application.Exercises;

public sealed record StudentQuestionDto(
    long Id,
    string Text,
    QuestionType Type,
    QuestionPlacement Placement,
    int? VideoTimestampSeconds,
    bool Passed,
    IReadOnlyList<StudentQuestionOptionDto> Options);
