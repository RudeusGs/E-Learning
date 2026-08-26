namespace Elearning.Domain;

public sealed class StudentAnswer
{
    private StudentAnswer()
    {
    }

    public long Id { get; private set; }
    public long StudentId { get; private set; }
    public long QuestionId { get; private set; }
    public long OptionId { get; private set; }
    public bool IsCorrect { get; private set; }
    public DateTimeOffset AnsweredAtUtc { get; private set; }
    public Question Question { get; private set; } = null!;
    public QuestionOption Option { get; private set; } = null!;

    public static StudentAnswer Create(
        long studentId,
        long questionId,
        long optionId,
        bool correctnessSnapshot,
        DateTimeOffset now)
    {
        if (studentId <= 0 || questionId <= 0 || optionId <= 0)
        {
            throw new DomainValidationException("Câu trả lời yêu cầu học viên, câu hỏi và lựa chọn hợp lệ.");
        }

        return new StudentAnswer
        {
            StudentId = studentId,
            QuestionId = questionId,
            OptionId = optionId,
            IsCorrect = correctnessSnapshot,
            AnsweredAtUtc = now
        };
    }
}
