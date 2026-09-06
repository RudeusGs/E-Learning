
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
    public int? VideoDurationSeconds { get; private set; }
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
        DateTimeOffset now,
        int? videoDurationSeconds = null)
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
            now,
            videoDurationSeconds);
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
        DateTimeOffset now,
        int? videoDurationSeconds = null)
    {
        ValidateDetails(
            title,
            videoProvider,
            videoExternalId,
            videoDurationSeconds,
            sortOrder,
            status);

        Title = title.Trim();
        Description = NormalizeOptional(description);
        ContentHtml = NormalizeOptional(sanitizedContentHtml);
        VideoProvider = videoProvider;
        VideoExternalId = NormalizeOptional(videoExternalId);
        VideoDurationSeconds = videoDurationSeconds;
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
        int? videoDurationSeconds,
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

        if (videoProvider is null && videoDurationSeconds is not null)
        {
            throw new DomainValidationException("Không được cấu hình thời lượng khi bài học không có video.");
        }

        if (videoDurationSeconds is <= 0 or > 43200)
        {
            throw new DomainValidationException("Thời lượng video phải nằm trong khoảng 1 giây đến 12 giờ.");
        }

        if (status == LessonStatus.Published && videoProvider is not null && videoDurationSeconds is null)
        {
            throw new DomainValidationException(
                "Bài học có video phải có thời lượng video trước khi xuất bản.");
        }

        if (Status == LessonStatus.Archived && status != LessonStatus.Archived)
        {
            throw new DomainValidationException("Không thể kích hoạt lại bài học đã lưu trữ thông qua việc cập nhật.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
