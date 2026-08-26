using Elearning.Application.Progress;

namespace Elearning.Application.Lessons;

public interface ILessonProgressCommandHandler
{
    Task<LessonProgressDto> ExecuteAsync(StartLessonCommand command, CancellationToken cancellationToken);
    Task<CompleteLessonDto> ExecuteAsync(CompleteLessonCommand command, CancellationToken cancellationToken);
}
