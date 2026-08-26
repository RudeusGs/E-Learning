using Elearning.Application.Exceptions;

namespace Elearning.Application.Security;

public static class ExternalUrlValidator
{
    public static string? NormalizeOptionalHttpsUrl(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > 2048 ||
            !Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(
                $"Invalid {fieldName}",
                $"{fieldName} must be an absolute HTTPS URL no longer than 2048 characters.");
        }

        return uri.AbsoluteUri;
    }
}
