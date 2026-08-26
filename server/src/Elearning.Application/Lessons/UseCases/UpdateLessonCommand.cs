namespace Elearning.Application.Lessons;

public sealed record UpdateLessonCommand(long LessonId, LessonWriteRequest Lesson);
