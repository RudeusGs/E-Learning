using Elearning.Application.Courses;
using Elearning.Application.Exceptions;
using Elearning.Application.Lessons;
using Elearning.Domain;
using Elearning.Infrastructure.Authorization;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Courses;

public sealed class StudentCourseDetailQueryHandler(
    ElearningDbContext dbContext,
    ActiveStudentPolicy activeStudentPolicy) : IStudentCourseDetailQueryHandler
{
    public async Task<StudentCourseDetailDto> ExecuteAsync(
        GetStudentCourseQuery query,
        CancellationToken cancellationToken)
    {
        await activeStudentPolicy.EnsureSatisfiedAsync(query.StudentId, cancellationToken);
        var course = await LoadCourseAsync(query.StudentId, query.CourseId, cancellationToken);
        var lessons = await LoadLessonsAsync(query.StudentId, query.CourseId, cancellationToken);
        return CreateResponse(course, lessons);
    }

    private async Task<CourseProjection> LoadCourseAsync(
        long studentId,
        long courseId,
        CancellationToken cancellationToken) =>
        await dbContext.Courses
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id == courseId &&
                candidate.Status == CourseStatus.Published &&
                candidate.Enrollments.Any(enrollment =>
                    enrollment.StudentId == studentId &&
                    enrollment.Status == EnrollmentStatus.Active))
            .Select(candidate => new CourseProjection(candidate.Id, candidate.Title, candidate.Description))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException("Course");

    private Task<List<LessonProjection>> LoadLessonsAsync(
        long studentId,
        long courseId,
        CancellationToken cancellationToken) =>
        dbContext.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.CourseId == courseId && lesson.Status == LessonStatus.Published)
            .OrderBy(lesson => lesson.SortOrder)
            .ThenBy(lesson => lesson.Id)
            .Select(lesson => new LessonProjection(
                lesson.Id,
                lesson.Title,
                lesson.SortOrder,
                lesson.Progress.Any(progress =>
                    progress.StudentId == studentId &&
                    progress.Status == LessonProgressStatus.Completed)))
            .ToListAsync(cancellationToken);

    private static StudentCourseDetailDto CreateResponse(
        CourseProjection course,
        IReadOnlyList<LessonProjection> rows)
    {
        var lessons = new List<StudentLessonSummaryDto>(rows.Count);
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var canAccess = index == 0 || rows[index - 1].Completed;
            var state = row.Completed
                ? LessonAccessStates.Completed
                : canAccess
                    ? LessonAccessStates.Available
                    : LessonAccessStates.Locked;
            lessons.Add(new StudentLessonSummaryDto(row.Id, row.Title, row.SortOrder, state, canAccess));
        }

        var progress = CourseProgress.Calculate(rows.Count(row => row.Completed), rows.Count);
        return new StudentCourseDetailDto(
            course.Id,
            course.Title,
            course.Description,
            progress.CompletedLessons,
            progress.TotalLessons,
            progress.Percentage,
            lessons);
    }

    private sealed record CourseProjection(long Id, string Title, string? Description);
    private sealed record LessonProjection(long Id, string Title, int SortOrder, bool Completed);
}
