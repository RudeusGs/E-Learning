namespace Elearning.Domain;

public sealed class Enrollment
{
    private Enrollment()
    {
    }

    public long Id { get; private set; }
    public long StudentId { get; private set; }
    public long CourseId { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public DateTimeOffset EnrolledAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Course Course { get; private set; } = null!;

    public static Enrollment Create(long studentId, long courseId, DateTimeOffset now)
    {
        if (studentId <= 0 || courseId <= 0)
        {
            throw new DomainValidationException("Đăng ký khóa học yêu cầu học viên và khóa học hợp lệ.");
        }

        return new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            Status = EnrollmentStatus.Active,
            EnrolledAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Reactivate(DateTimeOffset now)
    {
        Status = EnrollmentStatus.Active;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        Status = EnrollmentStatus.Inactive;
        UpdatedAtUtc = now;
    }
}
