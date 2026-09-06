
namespace Elearning.Api.Contracts.Lessons.Responses;

public sealed record VideoProgressResponse(
    int MaxPositionSeconds,
    int? DurationSeconds,
    bool Completed,
    long? BlockedByQuestionId);
