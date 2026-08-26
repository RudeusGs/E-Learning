namespace Elearning.Domain;

public sealed class Course
{
    private Course()
    {
    }

    public long Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public CourseStatus Status { get; private set; }
    public int SortOrder { get; private set; }
    public long Version { get; private set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public ICollection<Lesson> Lessons { get; } = [];
    public ICollection<Enrollment> Enrollments { get; } = [];

    public static Course Create(
        string title,
        string? description,
        string? thumbnailUrl,
        int sortOrder,
        CourseStatus status,
        DateTimeOffset now)
    {
        var course = new Course
        {
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        course.UpdateDetails(title, description, thumbnailUrl, sortOrder, status, now);
        return course;
    }

    public void UpdateDetails(
        string title,
        string? description,
        string? thumbnailUrl,
        int sortOrder,
        CourseStatus status,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Tiêu đề khóa học là bắt buộc.");
        }

        if (title.Trim().Length > 200)
        {
            throw new DomainValidationException("Tiêu đề khóa học không được vượt quá 200 ký tự.");
        }

        if (sortOrder < 0)
        {
            throw new DomainValidationException("Thứ tự sắp xếp khóa học không được là số âm.");
        }

        if (Status == CourseStatus.Archived && status != CourseStatus.Archived)
        {
            throw new DomainValidationException("Không thể kích hoạt lại khóa học đã lưu trữ thông qua việc cập nhật.");
        }

        Title = title.Trim();
        Description = NormalizeOptional(description);
        ThumbnailUrl = NormalizeOptional(thumbnailUrl);
        SortOrder = sortOrder;
        Status = status;
        UpdatedAtUtc = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = CourseStatus.Archived;
        UpdatedAtUtc = now;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
