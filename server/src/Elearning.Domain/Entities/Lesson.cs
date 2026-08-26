namespace Elearning.Domain;

public sealed class Lesson
{
    private Lesson()
    {
    }

    public long Id { get; private set; }
    public long CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ContentHtml { get; private set; }
    public VideoProvider? VideoProvider { get; private set; }
    public string? VideoExternalId { get; private set; }
    public int SortOrder { get; private set; }
    public LessonStatus Status { get; private set; }
    public long Version { get; private set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Course Course { get; private set; } = null!;
    public ICollection<Question> Questions { get; } = [];
    public ICollection<LessonProgress> Progress { get; } = [];

    public static Lesson Create(
        long courseId,
        string title,
        string? description,
        string? sanitizedContentHtml,
        VideoProvider? videoProvider,
        string? videoExternalId,
        int sortOrder,
        LessonStatus status,
        DateTimeOffset now)
    {
        if (courseId <= 0)
        {
            throw new DomainValidationException("Bài học phải thuộc về một khóa học hợp lệ.");
        }

        var lesson = new Lesson
        {
            CourseId = courseId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        lesson.UpdateDetails(
            title,
            description,
            sanitizedContentHtml,
            videoProvider,
            videoExternalId,
            sortOrder,
            status,
            now);
        return lesson;
    }

    public void UpdateDetails(
        string title,
        string? description,
        string? sanitizedContentHtml,
        VideoProvider? videoProvider,
        string? videoExternalId,
        int sortOrder,
        LessonStatus status,
        DateTimeOffset now)
    {
        ValidateDetails(title, videoProvider, videoExternalId, sortOrder, status);

        Title = title.Trim();
        Description = NormalizeOptional(description);
        ContentHtml = NormalizeOptional(sanitizedContentHtml);
        VideoProvider = videoProvider;
        VideoExternalId = NormalizeOptional(videoExternalId);
        SortOrder = sortOrder;
        Status = status;
        UpdatedAtUtc = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = LessonStatus.Archived;
        UpdatedAtUtc = now;
    }

    private void ValidateDetails(
        string title,
        VideoProvider? videoProvider,
        string? videoExternalId,
        int sortOrder,
        LessonStatus status)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Tiêu đề bài học là bắt buộc.");
        }

        if (title.Trim().Length > 200)
        {
            throw new DomainValidationException("Tiêu đề bài học không được vượt quá 200 ký tự.");
        }

        if (sortOrder < 0)
        {
            throw new DomainValidationException("Thứ tự sắp xếp bài học không được là số âm.");
        }

        if ((videoProvider is null) != (videoExternalId is null))
        {
            throw new DomainValidationException("Nhà cung cấp video và ID bên ngoài phải được cung cấp cùng nhau.");
        }

        if (Status == LessonStatus.Archived && status != LessonStatus.Archived)
        {
            throw new DomainValidationException("Không thể kích hoạt lại bài học đã lưu trữ thông qua việc cập nhật.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
