
using Elearning.Application.Lessons;
using Elearning.Domain;

namespace Elearning.Infrastructure.Lessons;

internal static class LessonMapper
{
    public static LessonAdminDto ToAdminDto(Lesson lesson) =>
        new(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Description,
            lesson.ContentHtml,
            lesson.VideoProvider is not null && lesson.VideoExternalId is not null
                ? new VideoDto(
                    lesson.VideoProvider.Value,
                    lesson.VideoExternalId)
                : null,
            lesson.VideoDurationSeconds,
            lesson.SortOrder,
            lesson.Status,
            lesson.Version);
}
