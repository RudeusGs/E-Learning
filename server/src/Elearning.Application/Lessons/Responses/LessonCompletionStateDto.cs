
namespace Elearning.Application.Lessons;

public sealed record LessonCompletionStateDto(
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
