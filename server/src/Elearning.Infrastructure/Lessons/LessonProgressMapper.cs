using Elearning.Application.Progress;
using Elearning.Domain;

namespace Elearning.Infrastructure.Lessons;

internal static class LessonProgressMapper
{
    public static LessonProgressDto ToDto(LessonProgress progress) =>
        new(
            ((LessonProgressStatus?)progress.Status).ToContractValue(),
            progress.StartedAtUtc,
            progress.CompletedAtUtc);
}