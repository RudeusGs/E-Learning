using Elearning.Application.Exceptions;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Authorization;

public sealed class StudentLessonAccessPolicy(ElearningDbContext dbContext)
{
    public async Task<StudentLessonAccess> AuthorizeAsync(
        long studentId,
        long lessonId,
        CancellationToken cancellationToken)
    {
        var scope = await LoadScopeAsync(studentId, lessonId, cancellationToken);
        EnsurePublishedAndEnrolled(scope);
        await EnsurePredecessorCompletedAsync(studentId, scope!.PreviousLessonId, cancellationToken);
        return new StudentLessonAccess(scope.Id, scope.CourseId, scope.SortOrder, scope.PreviousLessonId);
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
                    enrollment.Status == EnrollmentStatus.Active),
                dbContext.Lessons
                    .Where(previous =>
                        previous.CourseId == lesson.CourseId &&
                        previous.Status == LessonStatus.Published &&
                        (previous.SortOrder < lesson.SortOrder ||
                         (previous.SortOrder == lesson.SortOrder && previous.Id < lesson.Id)))
                    .OrderByDescending(previous => previous.SortOrder)
                    .ThenByDescending(previous => previous.Id)
                    .Select(previous => (long?)previous.Id)
                    .FirstOrDefault()))
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
        bool HasEnrollment,
        long? PreviousLessonId);
}
