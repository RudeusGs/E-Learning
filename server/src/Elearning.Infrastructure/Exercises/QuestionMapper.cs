using Elearning.Application.Exercises;
using Elearning.Domain;

namespace Elearning.Infrastructure.Exercises;

internal static class QuestionMapper
{
    public static IReadOnlyCollection<QuestionOptionDraft> ToDrafts(QuestionWriteRequest request) =>
        request.Options
            .Select(option => new QuestionOptionDraft(option.Content, option.IsCorrect, option.SortOrder))
            .ToList();

    public static AdminQuestionDto ToAdminDto(Question question) =>
        new(
            question.Id,
            question.LessonId,
            question.Text,
            question.Type,
            question.Explanation,
            question.SortOrder,
            question.Version,
            question.Options
                .OrderBy(option => option.SortOrder)
                .Select(option => new AdminQuestionOptionDto(
                    option.Id,
                    option.Content,
                    option.IsCorrect,
                    option.SortOrder))
                .ToList());
}
