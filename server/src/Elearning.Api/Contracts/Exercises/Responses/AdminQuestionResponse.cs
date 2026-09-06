
using Elearning.Domain;

namespace Elearning.Api.Contracts.Exercises.Responses;

public sealed record AdminQuestionResponse(
    long Id,
    long LessonId,
    string Text,
    QuestionType Type,
    QuestionPlacement Placement,
    int? VideoTimestampSeconds,
    string? Explanation,
    int SortOrder,
    long Version,
    IReadOnlyList<AdminQuestionOptionResponse> Options);
