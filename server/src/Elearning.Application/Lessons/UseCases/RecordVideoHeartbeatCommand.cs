
namespace Elearning.Application.Lessons;

public sealed record RecordVideoHeartbeatCommand(
    long StudentId,
    long LessonId,
    int PositionSeconds);
