namespace Elearning.Application.Courses;

public interface IAdminCourseCommandHandler
{
    Task<CourseDto> ExecuteAsync(CreateCourseCommand command, CancellationToken cancellationToken);
    Task<CourseDto> ExecuteAsync(UpdateCourseCommand command, CancellationToken cancellationToken);
    Task ExecuteAsync(ArchiveCourseCommand command, CancellationToken cancellationToken);
}
