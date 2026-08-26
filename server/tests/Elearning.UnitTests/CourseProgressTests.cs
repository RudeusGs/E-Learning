using Elearning.Domain;
using Xunit;

namespace Elearning.UnitTests;

public sealed class CourseProgressTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 5, 0)]
    [InlineData(1, 5, 20)]
    [InlineData(2, 5, 40)]
    [InlineData(5, 5, 100)]
    public void CalculateReturnsExpectedPercentage(int completed, int total, int expected)
    {
        var progress = CourseProgress.Calculate(completed, total);

        Assert.Equal(expected, progress.Percentage);
        Assert.Equal(completed, progress.CompletedLessons);
        Assert.Equal(total, progress.TotalLessons);
    }

    [Fact]
    public void CalculateRejectsInconsistentCounts()
    {
        Assert.Throws<DomainValidationException>(() => CourseProgress.Calculate(2, 1));
    }
}
