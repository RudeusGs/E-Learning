namespace Elearning.Domain;

public sealed class QuestionOption
{
    private QuestionOption()
    {
    }

    public long Id { get; private set; }
    public long QuestionId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }
    public int SortOrder { get; private set; }
    public Question Question { get; private set; } = null!;

    internal static QuestionOption Create(QuestionOptionDraft draft) => new()
    {
        Content = draft.Content.Trim(),
        IsCorrect = draft.IsCorrect,
        SortOrder = draft.SortOrder
    };
}
