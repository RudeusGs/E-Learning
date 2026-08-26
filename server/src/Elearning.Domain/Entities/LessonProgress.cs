namespace Elearning.Domain;

public sealed class LessonProgress
{
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
        return new DateTimeOffset(utcTicks - (utcTicks % TimeSpan.TicksPerMicrosecond), TimeSpan.Zero);
    }
}
