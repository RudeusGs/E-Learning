using Elearning.Domain;
using Xunit;

namespace Elearning.UnitTests;

public sealed class CourseTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateNormalizesTitleAndInitializesAuditState()
    {
        var course = Course.Create("  Python Basic  ", null, null, 1, CourseStatus.Draft, Now);

        Assert.Equal("Python Basic", course.Title);
        Assert.Equal(Now, course.CreatedAtUtc);
        Assert.Equal(Now, course.UpdatedAtUtc);
        Assert.Equal(1, course.Version);
    }

    [Fact]
    public void ArchivedCourseCannotBeReactivatedThroughUpdate()
    {
        var course = Course.Create("Python", null, null, 1, CourseStatus.Draft, Now);
        course.Archive(Now.AddMinutes(1));

        Assert.Throws<DomainValidationException>(() => course.UpdateDetails(
            "Python",
            null,
            null,
            1,
            CourseStatus.Published,
            Now.AddMinutes(2)));
    }
}
