namespace Elearning.Domain;

public sealed class LessonProgress
{
    public const int VideoEndToleranceSeconds = 2;
    private const int MinimumHeartbeatAdvanceWindowSeconds = 1;
    private const int MaximumTrustedHeartbeatGapSeconds = 60;

    private LessonProgress()
    {
    }

    public long Id { get; private set; }
    public long StudentId { get; private set; }
    public long LessonId { get; private set; }
    public LessonProgressStatus Status { get; private set; }
    public long Version { get; private set; } = 1;
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public int VideoMaxPositionSeconds { get; private set; }
    public int? VideoLastPositionSeconds { get; private set; }
    public DateTimeOffset? VideoHeartbeatAtUtc { get; private set; }
    public DateTimeOffset? VideoCompletedAtUtc { get; private set; }

    public Lesson Lesson { get; private set; } = null!;

    public static LessonProgress Start(long studentId, long lessonId, DateTimeOffset now)
    {
        ValidateIdentity(studentId, lessonId);
        now = NormalizeTimestamp(now);
        return new LessonProgress
        {
            StudentId = studentId,
            LessonId = lessonId,
            Status = LessonProgressStatus.InProgress,
            StartedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public static LessonProgress CompleteWithoutStart(long studentId, long lessonId, DateTimeOffset now)
    {
        var progress = Start(studentId, lessonId, now);
        progress.Complete(now);
        return progress;
    }

    public void BeginVideoTracking(DateTimeOffset now)
    {
        if (VideoHeartbeatAtUtc is not null)
        {
            return;
        }

        now = NormalizeTimestamp(now);
        VideoHeartbeatAtUtc = now;
        VideoLastPositionSeconds = VideoMaxPositionSeconds;
        UpdatedAtUtc = now;
    }

    public int RecordVideoHeartbeat(
        int reportedPositionSeconds,
        int videoDurationSeconds,
        int maximumAllowedPositionSeconds,
        DateTimeOffset now)
    {
        if (reportedPositionSeconds < 0)
        {
            throw new DomainValidationException("Vị trí video không được là số âm.");
        }

        if (videoDurationSeconds <= 0)
        {
            throw new DomainValidationException("Thời lượng video phải lớn hơn 0.");
        }

        now = NormalizeTimestamp(now);
        BeginVideoTracking(now);

        var trustedGate = Math.Clamp(maximumAllowedPositionSeconds, 0, videoDurationSeconds);
        var reported = Math.Clamp(reportedPositionSeconds, 0, videoDurationSeconds);
        var elapsedSeconds = Math.Max(
            0,
            (now - VideoHeartbeatAtUtc!.Value).TotalSeconds);

        var accepted = reported;

        if (reported > VideoMaxPositionSeconds + 1)
        {
            if (elapsedSeconds < MinimumHeartbeatAdvanceWindowSeconds)
            {
                accepted = VideoMaxPositionSeconds;
            }
            else
            {
                var trustedElapsed = Math.Min(
                    elapsedSeconds,
                    MaximumTrustedHeartbeatGapSeconds);
                var maxAdvance = Math.Max(1, (int)Math.Floor(trustedElapsed * 1.05d));
                accepted = Math.Min(
                    reported,
                    VideoMaxPositionSeconds + maxAdvance);
            }
        }

        accepted = Math.Min(accepted, trustedGate);

        if (accepted > VideoMaxPositionSeconds)
        {
            VideoMaxPositionSeconds = accepted;
        }

        VideoLastPositionSeconds = accepted;
        VideoHeartbeatAtUtc = now;

        if (
            VideoCompletedAtUtc is null &&
            trustedGate >= Math.Max(0, videoDurationSeconds - VideoEndToleranceSeconds) &&
            VideoMaxPositionSeconds >= Math.Max(0, videoDurationSeconds - VideoEndToleranceSeconds))
        {
            VideoCompletedAtUtc = now;
        }

        UpdatedAtUtc = now;
        return accepted;
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status == LessonProgressStatus.Completed)
        {
            return;
        }

        now = NormalizeTimestamp(now);
        Status = LessonProgressStatus.Completed;
        CompletedAtUtc = now;
        UpdatedAtUtc = now;
    }

    private static void ValidateIdentity(long studentId, long lessonId)
    {
        if (studentId <= 0 || lessonId <= 0)
        {
            throw new DomainValidationException("Tiến độ bài học yêu cầu học viên và bài học hợp lệ.");
        }
    }

    private static DateTimeOffset NormalizeTimestamp(DateTimeOffset value)
    {
        var utcTicks = value.UtcTicks;
        return new DateTimeOffset(
            utcTicks - (utcTicks % TimeSpan.TicksPerMicrosecond),
            TimeSpan.Zero);
    }
}
