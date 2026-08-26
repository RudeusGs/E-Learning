using Elearning.Application.Lessons;

namespace Elearning.Application.Security;

public interface IVideoNormalizer
{
    VideoDto? Normalize(string? videoUrl);
}
