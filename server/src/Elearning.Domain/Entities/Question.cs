
namespace Elearning.Domain;

public sealed class Question
{
    private readonly List<QuestionOption> _options = [];

    private Question()
    {
    }

    public long Id { get; private set; }
    public long LessonId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public QuestionType Type { get; private set; }
    public QuestionPlacement Placement { get; private set; } = QuestionPlacement.Reinforcement;
    public int? VideoTimestampSeconds { get; private set; }
    public string? Explanation { get; private set; }
    public int SortOrder { get; private set; }
    public long Version { get; private set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Lesson Lesson { get; private set; } = null!;
    public IReadOnlyCollection<QuestionOption> Options => _options;

    public static Question Create(
        long lessonId,
        string text,
        QuestionType type,
        string? explanation,
        int sortOrder,
        IReadOnlyCollection<QuestionOptionDraft> options,
        DateTimeOffset now,
        QuestionPlacement placement = QuestionPlacement.Reinforcement,
        int? videoTimestampSeconds = null)
    {
        if (lessonId <= 0)
        {
            throw new DomainValidationException("Câu hỏi phải thuộc về một bài học hợp lệ.");
        }

        var question = new Question
        {
            LessonId = lessonId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        question.Update(
            text,
            type,
            explanation,
            sortOrder,
            options,
            now,
            placement,
            videoTimestampSeconds);
        return question;
    }

    public void Update(
        string text,
        QuestionType type,
        string? explanation,
        int sortOrder,
        IReadOnlyCollection<QuestionOptionDraft> options,
        DateTimeOffset now,
        QuestionPlacement placement = QuestionPlacement.Reinforcement,
        int? videoTimestampSeconds = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainValidationException("Nội dung câu hỏi là bắt buộc.");
        }

        if (text.Trim().Length > 2000)
        {
            throw new DomainValidationException("Nội dung câu hỏi không được vượt quá 2000 ký tự.");
        }

        if (sortOrder < 0)
        {
            throw new DomainValidationException("Thứ tự sắp xếp câu hỏi không được là số âm.");
        }

        ValidatePlacement(placement, videoTimestampSeconds);
        ValidateOptions(type, options);

        Text = text.Trim();
        Type = type;
        Placement = placement;
        VideoTimestampSeconds = videoTimestampSeconds;
        Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();
        SortOrder = sortOrder;
        UpdatedAtUtc = now;
        _options.Clear();
        _options.AddRange(options.OrderBy(option => option.SortOrder).Select(QuestionOption.Create));
    }

    private static void ValidatePlacement(
        QuestionPlacement placement,
        int? videoTimestampSeconds)
    {
        if (placement == QuestionPlacement.Reinforcement)
        {
            if (videoTimestampSeconds is not null)
            {
                throw new DomainValidationException(
                    "Câu hỏi củng cố cuối bài không được có mốc thời gian video.");
            }

            return;
        }

        if (videoTimestampSeconds is null or < 1)
        {
            throw new DomainValidationException(
                "Câu hỏi trong video phải có mốc thời gian từ 1 giây trở lên.");
        }
    }

    private static void ValidateOptions(
        QuestionType type,
        IReadOnlyCollection<QuestionOptionDraft> options)
    {
        if (options.Count(option => option.IsCorrect) != 1)
        {
            throw new DomainValidationException("Câu hỏi phải có chính xác một đáp án đúng.");
        }

        if (options.Any(option => string.IsNullOrWhiteSpace(option.Content)))
        {
            throw new DomainValidationException("Các lựa chọn của câu hỏi không được để trống.");
        }

        if (options.Select(option => option.SortOrder).Distinct().Count() != options.Count)
        {
            throw new DomainValidationException("Thứ tự của các lựa chọn câu hỏi phải là duy nhất.");
        }

        if (type == QuestionType.MultipleChoice && options.Count is < 2 or > 4)
        {
            throw new DomainValidationException("Câu hỏi nhiều lựa chọn phải có từ 2 đến 4 lựa chọn.");
        }

        if (type == QuestionType.TrueFalse && options.Count != 2)
        {
            throw new DomainValidationException("Câu hỏi Đúng/Sai phải có đúng 2 lựa chọn.");
        }
    }
}
