namespace Elearning.Application.Lessons;

public interface IAdminLessonCommandHandler
{
    Task<LessonAdminDto> ExecuteAsync(CreateLessonCommand command, CancellationToken cancellationToken);
    Task<LessonAdminDto> ExecuteAsync(UpdateLessonCommand command, CancellationToken cancellationToken);
    Task ExecuteAsync(ArchiveLessonCommand command, CancellationToken cancellationToken);
}
