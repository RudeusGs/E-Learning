
namespace Elearning.Api.Contracts.Lessons.Responses;

public sealed record LessonCompletionStateResponse(
    bool VideoRequired,
    bool VideoConfigured,
    bool VideoCompleted,
    bool ReinforcementUnlocked,
    int CheckpointTotal,
    int CheckpointPassed,
    int ReinforcementTotal,
    int ReinforcementPassed,
    int ReinforcementScorePercent,
    int RequiredScorePercent,
    bool CanComplete);
