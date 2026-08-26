namespace Elearning.Application.Students;

public interface IStudentAccountCommandHandler
{
    Task<StudentDetailDto> ExecuteAsync(CreateStudentCommand command, CancellationToken cancellationToken);
    Task<StudentDetailDto> ExecuteAsync(UpdateStudentCommand command, CancellationToken cancellationToken);
    Task ExecuteAsync(DisableStudentCommand command, CancellationToken cancellationToken);
}
