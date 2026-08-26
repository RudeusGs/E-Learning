namespace Elearning.Application.Students;

public sealed record DisableStudentCommand(long StudentId, long ActorUserId);
