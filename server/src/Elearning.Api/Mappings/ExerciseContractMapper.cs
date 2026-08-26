using Elearning.Api.Contracts.Exercises.Responses;
using Elearning.Application.Exercises;
using ApiAnswerRequest = Elearning.Api.Contracts.Exercises.Requests.AnswerRequest;
using ApiQuestionWriteRequest = Elearning.Api.Contracts.Exercises.Requests.QuestionWriteRequest;

namespace Elearning.Api.Mappings;

public static class ExerciseContractMapper
{
    public static IReadOnlyList<AdminQuestionResponse> ToResponse(
        this IReadOnlyList<AdminQuestionDto> questions) =>
        questions.Select(ToResponse).ToList();

    public static AnswerRequest ToApplication(this ApiAnswerRequest request) => new(request.OptionId);

    public static QuestionWriteRequest ToApplication(this ApiQuestionWriteRequest request) =>
        new(
            request.Text,
            request.Type,
            request.Explanation,
            request.SortOrder,
            request.Options.Select(option => new QuestionOptionWriteRequest(
                option.Content,
                option.IsCorrect,
                option.SortOrder)).ToList(),
            request.Version);

    public static AdminQuestionResponse ToResponse(this AdminQuestionDto question) =>
        new(
            question.Id,
            question.LessonId,
            question.Text,
            question.Type,
            question.Explanation,
            question.SortOrder,
            question.Version,
            question.Options.Select(option => new AdminQuestionOptionResponse(
                option.Id,
                option.Content,
                option.IsCorrect,
                option.SortOrder)).ToList());

    public static AnswerResultResponse ToResponse(this AnswerResultDto answer) =>
        new(answer.Correct, answer.Explanation, answer.AnsweredAtUtc);

    public static StudentQuestionResponse ToResponse(this StudentQuestionDto question) =>
        new(
            question.Id,
            question.Text,
            question.Type,
            question.Options.Select(option => new StudentQuestionOptionResponse(
                option.Id,
                option.Content)).ToList());
}
