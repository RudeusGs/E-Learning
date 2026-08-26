namespace Elearning.Application.Lessons;

public sealed record CreateLessonCommand(long CourseId, LessonWriteRequest Lesson);
