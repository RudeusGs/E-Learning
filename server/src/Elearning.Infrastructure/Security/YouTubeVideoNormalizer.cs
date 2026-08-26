using System.Text.RegularExpressions;
using Elearning.Application.Exceptions;
using Elearning.Application.Lessons;
using Elearning.Application.Security;
using Elearning.Domain;

namespace Elearning.Infrastructure.Security;

public sealed partial class YouTubeVideoNormalizer : IVideoNormalizer
{
    public VideoDto? Normalize(string? videoUrl)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(videoUrl.Trim(), UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidVideo();
        }

        var host = uri.IdnHost.ToLowerInvariant();
        string? externalId = host switch
        {
            "youtu.be" => uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(),
            "youtube.com" or "www.youtube.com" or "m.youtube.com" => ExtractYouTubeId(uri),
            _ => null
        };

        if (externalId is null || !YouTubeIdPattern().IsMatch(externalId))
        {
            throw InvalidVideo();
        }

        return new VideoDto(VideoProvider.Youtube, externalId);
    }

    private static string? ExtractYouTubeId(Uri uri)
    {
        if (uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase))
        {
            return ParseQuery(uri.Query).GetValueOrDefault("v");
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 2 && segments[0] is "embed" or "shorts" ? segments[1] : null;
    }

    private static Dictionary<string, string> ParseQuery(string query) => query
        .TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Split('=', 2))
        .Where(parts => parts.Length == 2)
        .ToDictionary(parts => Uri.UnescapeDataString(parts[0]), parts => Uri.UnescapeDataString(parts[1]), StringComparer.OrdinalIgnoreCase);

    private static RequestValidationException InvalidVideo() =>
        new("Invalid video URL", "Only valid HTTPS YouTube URLs are supported.");

    [GeneratedRegex("^[A-Za-z0-9_-]{11}$", RegexOptions.CultureInvariant)]
    private static partial Regex YouTubeIdPattern();
}
