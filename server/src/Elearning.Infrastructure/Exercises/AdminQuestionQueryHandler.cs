
using Elearning.Application.Exceptions;
using Elearning.Application.Exercises;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Exercises;

public sealed class AdminQuestionQueryHandler(
    ElearningDbContext dbContext) : IAdminQuestionQueryHandler
{
    public async Task<IReadOnlyList<AdminQuestionDto>> ExecuteAsync(
        ListAdminQuestionsQuery query,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Lessons.AsNoTracking().AnyAsync(
                lesson => lesson.Id == query.LessonId,
                cancellationToken))
        {
            throw new ResourceNotFoundException("Lesson");
        }

        return await dbContext.Questions
            .AsNoTracking()
            .Where(question => question.LessonId == query.LessonId)
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.Id)
            .Select(question => new AdminQuestionDto(
                question.Id,
                question.LessonId,
                question.Text,
                question.Type,
                question.Placement,
                question.VideoTimestampSeconds,
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
                    .ToList()))
            .ToListAsync(cancellationToken);
    }
}
