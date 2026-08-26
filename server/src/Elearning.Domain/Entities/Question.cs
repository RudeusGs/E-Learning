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
        DateTimeOffset now)
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

        question.Update(text, type, explanation, sortOrder, options, now);
        return question;
    }

    public void Update(
        string text,
        QuestionType type,
        string? explanation,
        int sortOrder,
        IReadOnlyCollection<QuestionOptionDraft> options,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainValidationException("Nội dung câu hỏi là bắt buộc.");
        }

        if (sortOrder < 0)
        {
            throw new DomainValidationException("Thứ tự sắp xếp câu hỏi không được là số âm.");
        }

        ValidateOptions(type, options);

        Text = text.Trim();
        Type = type;
        Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();
        SortOrder = sortOrder;
        UpdatedAtUtc = now;
        _options.Clear();
        _options.AddRange(options.OrderBy(option => option.SortOrder).Select(QuestionOption.Create));
    }

    private static void ValidateOptions(QuestionType type, IReadOnlyCollection<QuestionOptionDraft> options)
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
            throw new DomainValidationException("Câu hỏi trắc nghiệm phải có từ hai đến bốn lựa chọn.");
        }

        if (type == QuestionType.TrueFalse && options.Count != 2)
        {
            throw new DomainValidationException("Câu hỏi đúng/sai phải có chính xác hai lựa chọn.");
        }
    }
}
