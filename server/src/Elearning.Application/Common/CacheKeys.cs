namespace Elearning.Application.Common;

public static class CacheKeys
{
    public const string AdminDashboard = "admin_dashboard";

    public static string AdminProgress(int limit, string? cursor, long? studentId, long? courseId, string? search)
        => $"admin_progress_{limit}_{cursor}_{studentId}_{courseId}_{search}";

    public static string StudentProgress(long studentId, long courseId)
        => $"student_progress_{studentId}_{courseId}";

    public static string StudentCourseDetail(long studentId, long courseId)
        => $"student_course_detail_{studentId}_{courseId}";

    public static string StudentCourses(long studentId, int limit, string? cursor, string? progress)
        => $"student_courses_{studentId}_{limit}_{cursor}_{progress}";
}
