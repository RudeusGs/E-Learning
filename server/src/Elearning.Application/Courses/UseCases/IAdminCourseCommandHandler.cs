namespace Elearning.Application.Courses;

public interface IAdminCourseCommandHandler
{
    Task<AdminCourseDetailDto> ExecuteAsync(CreateCourseCommand command, CancellationToken cancellationToken);
    Task<AdminCourseDetailDto> ExecuteAsync(UpdateCourseCommand command, CancellationToken cancellationToken);
    Task ExecuteAsync(ArchiveCourseCommand command, CancellationToken cancellationToken);
}
