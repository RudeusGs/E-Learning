namespace Elearning.Application.Students;

public sealed record UpdateStudentCommand(long StudentId, StudentUpdateRequest Student);
