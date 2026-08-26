using Elearning.Domain;
using Xunit;

namespace Elearning.UnitTests;

public sealed class LessonProgressTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 8, 25, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CompleteIsIdempotentAndPreservesFirstTimestamp()
    {
        var progress = LessonProgress.Start(10, 20, StartedAt);
        var firstCompletion = StartedAt.AddMinutes(10);
        progress.Complete(firstCompletion);

        progress.Complete(firstCompletion.AddHours(1));

        Assert.Equal(LessonProgressStatus.Completed, progress.Status);
        Assert.Equal(firstCompletion, progress.CompletedAtUtc);
        Assert.Equal(StartedAt, progress.StartedAtUtc);
    }

    [Fact]
    public void CompleteWithoutStartUsesOneConsistentTimestamp()
    {
        var progress = LessonProgress.CompleteWithoutStart(10, 20, StartedAt);

        Assert.Equal(StartedAt, progress.StartedAtUtc);
        Assert.Equal(StartedAt, progress.CompletedAtUtc);
    }
}
