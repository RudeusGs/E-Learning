using Elearning.Application.Exceptions;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Authorization;

public sealed class StudentLessonAccessPolicy(
    ElearningDbContext dbContext,
    ActiveStudentPolicy activeStudentPolicy)
{
    public async Task<StudentLessonAccess> AuthorizeAsync(
        long studentId,
        long lessonId,
        CancellationToken cancellationToken)
    {
        await activeStudentPolicy.EnsureSatisfiedAsync(studentId, cancellationToken);
        var scope = await LoadScopeAsync(studentId, lessonId, cancellationToken);
        EnsurePublishedAndEnrolled(scope);
        var previousLessonId = await FindPreviousLessonIdAsync(scope!, cancellationToken);
        await EnsurePredecessorCompletedAsync(studentId, previousLessonId, cancellationToken);
        return new StudentLessonAccess(scope!.Id, scope.CourseId, scope.SortOrder, previousLessonId);
    }

    private Task<LessonAccessScope?> LoadScopeAsync(
        long studentId,
        long lessonId,
        CancellationToken cancellationToken) =>
        dbContext.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.Id == lessonId)
            .Select(lesson => new LessonAccessScope(
                lesson.Id,
                lesson.CourseId,
                lesson.SortOrder,
                lesson.Status,
                lesson.Course.Status,
                lesson.Course.Enrollments.Any(enrollment =>
                    enrollment.StudentId == studentId &&
                    enrollment.Status == EnrollmentStatus.Active)))
            .SingleOrDefaultAsync(cancellationToken);

    private static void EnsurePublishedAndEnrolled(LessonAccessScope? scope)
    {
        if (scope is null ||
            scope.LessonStatus != LessonStatus.Published ||
            scope.CourseStatus != CourseStatus.Published ||
            !scope.HasEnrollment)
        {
            throw new ResourceNotFoundException("Lesson");
        }
    }

    private Task<long?> FindPreviousLessonIdAsync(
        LessonAccessScope scope,
        CancellationToken cancellationToken) =>
        dbContext.Lessons
            .AsNoTracking()
            .Where(lesson =>
                lesson.CourseId == scope.CourseId &&
                lesson.Status == LessonStatus.Published &&
                (lesson.SortOrder < scope.SortOrder ||
                 (lesson.SortOrder == scope.SortOrder && lesson.Id < scope.Id)))
            .OrderByDescending(lesson => lesson.SortOrder)
            .ThenByDescending(lesson => lesson.Id)
            .Select(lesson => (long?)lesson.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task EnsurePredecessorCompletedAsync(
        long studentId,
        long? previousLessonId,
        CancellationToken cancellationToken)
    {
        if (previousLessonId is not null)
        {
            var predecessorCompleted = await dbContext.LessonProgress
                .AsNoTracking()
                .AnyAsync(progress =>
                    progress.StudentId == studentId &&
                    progress.LessonId == previousLessonId &&
                    progress.Status == LessonProgressStatus.Completed,
                    cancellationToken);

            if (!predecessorCompleted)
            {
                throw AuthorizationException.LessonLocked();
            }
        }
    }

    private sealed record LessonAccessScope(
        long Id,
        long CourseId,
        int SortOrder,
        LessonStatus LessonStatus,
        CourseStatus CourseStatus,
        bool HasEnrollment);
}
