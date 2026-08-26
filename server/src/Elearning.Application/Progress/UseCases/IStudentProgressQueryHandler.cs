namespace Elearning.Application.Progress;

public interface IStudentProgressQueryHandler
{
    Task<StudentCourseProgressDto> ExecuteAsync(
        GetStudentProgressQuery query,
        CancellationToken cancellationToken);
}
