namespace Elearning.Application.Courses;

public interface IStudentCourseDetailQueryHandler
{
    Task<StudentCourseDetailDto> ExecuteAsync(
        GetStudentCourseQuery query,
        CancellationToken cancellationToken);
}
