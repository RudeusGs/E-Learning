using Elearning.Domain;

namespace Elearning.Application.Exercises;

public sealed record AdminQuestionDto(
    long Id,
    long LessonId,
    string Text,
    QuestionType Type,
    string? Explanation,
    int SortOrder,
    long Version,
    IReadOnlyList<AdminQuestionOptionDto> Options);
