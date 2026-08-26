namespace Elearning.Application.Courses;

public sealed record UpdateCourseCommand(long CourseId, CourseWriteRequest Course);
