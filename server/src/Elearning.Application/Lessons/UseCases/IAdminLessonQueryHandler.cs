namespace Elearning.Application.Lessons;

public interface IAdminLessonQueryHandler
{
    Task<IReadOnlyList<LessonAdminDto>> ExecuteAsync(
        ListAdminLessonsQuery query,
        CancellationToken cancellationToken);

    Task<LessonAdminDto> ExecuteAsync(GetAdminLessonQuery query, CancellationToken cancellationToken);
}
