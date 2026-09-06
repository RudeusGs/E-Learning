
namespace Elearning.Application.Lessons;

public sealed record VideoProgressDto(
    int MaxPositionSeconds,
    int? DurationSeconds,
    bool Completed,
    long? BlockedByQuestionId);
