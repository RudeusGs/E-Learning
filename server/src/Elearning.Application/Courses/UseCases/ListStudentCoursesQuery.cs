namespace Elearning.Application.Courses;

public sealed record ListStudentCoursesQuery(long StudentId, int Limit, string? Cursor);
