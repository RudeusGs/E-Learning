using Elearning.Application.Lessons;
using Xunit;

namespace Elearning.UnitTests;

public sealed class LessonCompletionPolicyTests
{
    [Theory]
    [InlineData(33, 41, true)]
    [InlineData(4, 5, false)]
    [InlineData(5, 6, true)]
    [InlineData(0, 0, true)]
    public void ReinforcementThresholdUsesExactRatio(int passed, int total, bool expected)
    {
        Assert.Equal(expected, LessonCompletionPolicy.IsReinforcementScorePassing(passed, total));
    }
}
