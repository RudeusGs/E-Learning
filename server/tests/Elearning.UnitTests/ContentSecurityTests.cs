using Elearning.Application.Exceptions;
using Elearning.Infrastructure.Security;
using Xunit;

namespace Elearning.UnitTests;

public sealed class ContentSecurityTests
{
    [Fact]
    public void SanitizerRemovesExecutableHtml()
    {
        var sanitizer = new HtmlContentSanitizer();

        var result = sanitizer.Sanitize(
            "<p onclick=\"alert(1)\">Safe</p><script>alert(1)</script><img src=x onerror=alert(1)><a href=\"javascript:alert(1)\">x</a>");

        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Safe</p>", result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    public void VideoNormalizerAcceptsSupportedYouTubeUrls(string url)
    {
        var result = new YouTubeVideoNormalizer().Normalize(url);

        Assert.NotNull(result);
        Assert.Equal("dQw4w9WgXcQ", result.ExternalId);
    }

    [Theory]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://evil.example/embed/dQw4w9WgXcQ")]
    [InlineData("javascript:alert(1)")]
    public void VideoNormalizerRejectsUnsupportedUrls(string url)
    {
        Assert.Throws<RequestValidationException>(() => new YouTubeVideoNormalizer().Normalize(url));
    }
}
