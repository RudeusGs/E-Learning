using Elearning.Application.Security;
using Ganss.Xss;

namespace Elearning.Infrastructure.Security;

public sealed class HtmlContentSanitizer : IContentSanitizer
{
    private static readonly string[] SafeTags =
    [
        "p", "br", "strong", "em", "ul", "ol", "li", "h1", "h2", "h3", "h4", "h5", "h6",
        "pre", "code", "blockquote", "a"
    ];

    private readonly HtmlSanitizer _sanitizer = CreateSanitizer();

    public string? Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        return _sanitizer.Sanitize(html).Trim();
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(SafeTags);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(["href", "title"]);
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["https", "mailto"]);
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedAtRules.Clear();
        return sanitizer;
    }
}
