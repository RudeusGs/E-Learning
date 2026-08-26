namespace Elearning.Application.Enrollments;

public interface IEnrollmentCommandHandler
{
    Task<EnrollmentDto> ExecuteAsync(
        EnrollStudentCommand command,
        CancellationToken cancellationToken);
}
