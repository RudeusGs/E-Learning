using Elearning.Application.Common;
using Elearning.Application.Common.Interfaces;
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
    ICacheService cacheService) : IStudentCourseDetailQueryHandler
{
    public async Task<StudentCourseDetailDto> ExecuteAsync(
        GetStudentCourseQuery query,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.StudentCourseDetail(query.StudentId, query.CourseId);
        var cachedResult = await cacheService.GetAsync<StudentCourseDetailDto>(cacheKey, cancellationToken);
        if (cachedResult is not null)
        {
            return cachedResult;
        }

        var projection = await LoadCourseAndLessonsAsync(query.StudentId, query.CourseId, cancellationToken);
        var result = CreateResponse(projection.Course, projection.Lessons);
        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
        return result;
    }

    private async Task<CourseDetailProjection> LoadCourseAndLessonsAsync(
        long studentId,
        long courseId,
        CancellationToken cancellationToken) =>
        // Assumption: Courses are naturally bounded by curriculum design to a reasonable number of lessons (e.g., < 200).
        // Returning all summary projections at once avoids complex pagination of sequential lock state.
        await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment =>
                enrollment.StudentId == studentId &&
                enrollment.CourseId == courseId &&
                enrollment.Status == EnrollmentStatus.Active &&
                enrollment.Course.Status == CourseStatus.Published)
            .Select(enrollment => new CourseDetailProjection(
                new CourseProjection(enrollment.Course.Id, enrollment.Course.Title, enrollment.Course.Description),
                enrollment.Course.Lessons
                    .Where(lesson => lesson.Status == LessonStatus.Published)
                    .OrderBy(lesson => lesson.SortOrder)
                    .ThenBy(lesson => lesson.Id)
                    .Select(lesson => new LessonProjection(
                        lesson.Id,
                        lesson.Title,
                        lesson.SortOrder,
                        lesson.Progress.Any(progress =>
                            progress.StudentId == studentId &&
                            progress.Status == LessonProgressStatus.Completed)))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException("Course");

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
    private sealed record CourseDetailProjection(CourseProjection Course, IReadOnlyList<LessonProjection> Lessons);
}
