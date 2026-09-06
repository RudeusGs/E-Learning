using Elearning.Application.Exceptions;
using Elearning.Application.Lessons;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Lessons;

public sealed class AdminLessonQueryHandler(ElearningDbContext dbContext) : IAdminLessonQueryHandler
{
    public async Task<IReadOnlyList<LessonAdminListDto>> ExecuteAsync(
        ListAdminLessonsQuery query,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Courses.AsNoTracking().AnyAsync(
                course => course.Id == query.CourseId,
                cancellationToken))
        {
            throw new ResourceNotFoundException("Course");
        }

        return await dbContext.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.CourseId == query.CourseId)
            .OrderBy(lesson => lesson.SortOrder)
            .ThenBy(lesson => lesson.Id)
            .Select(lesson => new LessonAdminListDto(
                lesson.Id,
                lesson.CourseId,
                lesson.Title,
                lesson.Description,
                lesson.SortOrder,
                lesson.Status,
                lesson.Version))
            .ToListAsync(cancellationToken);
    }

    public async Task<LessonAdminDto> ExecuteAsync(GetAdminLessonQuery query, CancellationToken cancellationToken)
    {
        var lesson = await dbContext.Lessons
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == query.LessonId, cancellationToken)
            ?? throw new ResourceNotFoundException("Lesson");
        return LessonMapper.ToAdminDto(lesson);
    }
}
