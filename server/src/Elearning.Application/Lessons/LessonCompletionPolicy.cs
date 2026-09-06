namespace Elearning.Application.Lessons;

public static class LessonCompletionPolicy
{
    public const int RequiredQuizScorePercent = 80;

    public static bool IsReinforcementScorePassing(
        int passed,
        int total,
        int requiredScorePercent = RequiredQuizScorePercent)
    {
        if (total == 0)
        {
            return true;
        }

        if (
            total < 0 ||
            passed < 0 ||
            passed > total ||
            requiredScorePercent is < 0 or > 100)
        {
            return false;
        }

        return (long)passed * 100 > (long)total * requiredScorePercent;
    }
}
