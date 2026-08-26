using Elearning.Domain;

namespace Elearning.Api.Contracts.Exercises.Responses;

public sealed record StudentQuestionResponse(
    long Id,
    string Text,
    QuestionType Type,
    IReadOnlyList<StudentQuestionOptionResponse> Options);
