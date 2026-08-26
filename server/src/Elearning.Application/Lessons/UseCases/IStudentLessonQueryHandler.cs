namespace Elearning.Application.Lessons;

public interface IStudentLessonQueryHandler
{
    Task<StudentLessonDto> ExecuteAsync(
        GetStudentLessonQuery query,
        CancellationToken cancellationToken);
}
