namespace Elearning.Application.Security;

public interface IContentSanitizer
{
    string? Sanitize(string? html);
}
