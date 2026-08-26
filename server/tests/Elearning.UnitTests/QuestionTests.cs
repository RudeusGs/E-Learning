using Elearning.Domain;
using Xunit;

namespace Elearning.UnitTests;

public sealed class QuestionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 2, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateAcceptsExactlyOneCorrectMultipleChoiceOption()
    {
        var question = Question.Create(
            1,
            "Which function prints output?",
            QuestionType.MultipleChoice,
            "print() outputs values.",
            1,
            [
                new QuestionOptionDraft("show()", false, 1),
                new QuestionOptionDraft("print()", true, 2)
            ],
            Now);

        Assert.Equal(2, question.Options.Count);
        Assert.Single(question.Options, option => option.IsCorrect);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CreateRejectsIncorrectCorrectOptionCount(int correctCount)
    {
        var options = new[]
        {
            new QuestionOptionDraft("A", correctCount > 0, 1),
            new QuestionOptionDraft("B", correctCount > 1, 2)
        };

        Assert.Throws<DomainValidationException>(() => Question.Create(
            1,
            "Question",
            QuestionType.MultipleChoice,
            null,
            1,
            options,
            Now));
    }

    [Fact]
    public void TrueFalseRequiresExactlyTwoOptions()
    {
        Assert.Throws<DomainValidationException>(() => Question.Create(
            1,
            "True?",
            QuestionType.TrueFalse,
            null,
            1,
            [new QuestionOptionDraft("True", true, 1)],
            Now));
    }
}
