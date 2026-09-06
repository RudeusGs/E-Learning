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

    [Fact]
    public void VideoHeartbeatCapsForwardJumpByElapsedWallTime()
    {
        var progress = LessonProgress.Start(10, 20, StartedAt);
        progress.BeginVideoTracking(StartedAt);

        var accepted = progress.RecordVideoHeartbeat(300, 600, 600, StartedAt.AddSeconds(30));

        Assert.InRange(accepted, 29, 31);
        Assert.Equal(accepted, progress.VideoMaxPositionSeconds);
    }

    [Fact]
    public void VideoHeartbeatAllowsOneMissedScheduledIntervalWithoutForcingReplay()
    {
        var progress = LessonProgress.Start(10, 20, StartedAt);
        progress.BeginVideoTracking(StartedAt);

        var accepted = progress.RecordVideoHeartbeat(60, 600, 600, StartedAt.AddSeconds(60));

        Assert.Equal(60, accepted);
        Assert.Equal(60, progress.VideoMaxPositionSeconds);
    }

    [Fact]
    public void VideoHeartbeatStillCapsAProlongedDisconnectedJump()
    {
        var progress = LessonProgress.Start(10, 20, StartedAt);
        progress.BeginVideoTracking(StartedAt);

        var accepted = progress.RecordVideoHeartbeat(120, 600, 600, StartedAt.AddSeconds(120));

        Assert.InRange(accepted, 62, 63);
        Assert.Equal(accepted, progress.VideoMaxPositionSeconds);
    }

    [Fact]
    public void ShortVideoCanCompleteAfterRealElapsedPlayback()
    {
        var progress = LessonProgress.Start(10, 20, StartedAt);
        progress.BeginVideoTracking(StartedAt);

        var accepted = progress.RecordVideoHeartbeat(4, 4, 4, StartedAt.AddSeconds(4));

        Assert.Equal(4, accepted);
        Assert.NotNull(progress.VideoCompletedAtUtc);
    }

    [Fact]
    public void VideoHeartbeatStopsAtPendingCheckpointGate()
    {
        var progress = LessonProgress.Start(10, 20, StartedAt);
        progress.BeginVideoTracking(StartedAt);

        var accepted = progress.RecordVideoHeartbeat(100, 600, 60, StartedAt.AddSeconds(90));

        Assert.Equal(60, accepted);
        Assert.Null(progress.VideoCompletedAtUtc);
    }
}
