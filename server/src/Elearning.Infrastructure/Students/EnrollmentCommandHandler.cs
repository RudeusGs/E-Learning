using Elearning.Application.Enrollments;
using Elearning.Application.Errors;
using Elearning.Application.Exceptions;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Elearning.Infrastructure.Students;

public sealed class EnrollmentCommandHandler(
    ElearningDbContext dbContext,
    StudentAccountStore students,
    TimeProvider timeProvider) : IEnrollmentCommandHandler
{
    public async Task<EnrollmentDto> ExecuteAsync(
        EnrollStudentCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Enrollment;
        await ValidateEnrollmentTargetsAsync(request, cancellationToken);
        var enrollment = await FindEnrollmentAsync(request, cancellationToken);
        return enrollment is null
            ? await CreateAsync(request, cancellationToken)
            : await ReactivateAsync(enrollment, cancellationToken);
    }

    private async Task ValidateEnrollmentTargetsAsync(
        EnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var student = await students.FindAsync(request.StudentId, tracked: false, cancellationToken);
        if (student.Status != AccountStatus.Active)
        {
            throw new ConflictException(ErrorCodes.AccountDisabled, "Cannot enroll a disabled student");
        }

        var courseStatus = await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Id == request.CourseId)
            .Select(course => (CourseStatus?)course.Status)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException("Course");
        if (courseStatus == CourseStatus.Archived)
        {
            throw new ConflictException(
                ErrorCodes.CourseArchived,
                "Archived courses cannot receive new enrollments");
        }
    }

    private Task<Enrollment?> FindEnrollmentAsync(
        EnrollmentRequest request,
        CancellationToken cancellationToken) =>
        dbContext.Enrollments.SingleOrDefaultAsync(
            item => item.StudentId == request.StudentId && item.CourseId == request.CourseId,
            cancellationToken);

    private async Task<EnrollmentDto> ReactivateAsync(
        Enrollment enrollment,
        CancellationToken cancellationToken)
    {
        enrollment.Reactivate(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return StudentMapper.ToEnrollment(enrollment);
    }

    private async Task<EnrollmentDto> CreateAsync(
        EnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var enrollment = Enrollment.Create(request.StudentId, request.CourseId, timeProvider.GetUtcNow());
        dbContext.Enrollments.Add(enrollment);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return StudentMapper.ToEnrollment(enrollment);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            enrollment = await dbContext.Enrollments.SingleAsync(
                item => item.StudentId == request.StudentId && item.CourseId == request.CourseId,
                cancellationToken);
            return enrollment.Status == EnrollmentStatus.Inactive
                ? await ReactivateAsync(enrollment, cancellationToken)
                : StudentMapper.ToEnrollment(enrollment);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
