using Elearning.Application.Auth.Security;
using Elearning.Application.Exceptions;
using Elearning.Application.Students;
using Elearning.Domain;
using Elearning.Infrastructure.Auth;
using Elearning.Infrastructure.Identity;
using Elearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Students;

public sealed class StudentAccountCommandHandler(
    ElearningDbContext dbContext,
    StudentAccountStore students,
    IAdminStudentQueryHandler studentQueries,
    UserManager<ApplicationUser> userManager,
    SessionRevoker sessionRevoker,
    SecurityAuditLogger auditLogger,
    TimeProvider timeProvider) : IStudentAccountCommandHandler
{
    private static readonly string StudentRoleName = UserRole.Student.ToIdentityName();

    public async Task<StudentDetailDto> ExecuteAsync(
        CreateStudentCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Student;
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            var now = timeProvider.GetUtcNow();
            var student = new ApplicationUser
            {
                Email = request.Email.Trim(),
                UserName = request.Email.Trim(),
                CreatedAtUtc = now
            };
            student.Rename(request.FullName, now);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            EnsureSucceeded(await userManager.CreateAsync(student, request.InitialPassword));
            EnsureSucceeded(await userManager.AddToRoleAsync(student, StudentRoleName));
            await transaction.CommitAsync(cancellationToken);
            return StudentMapper.ToDetail(student, []);
        });
    }

    public async Task<StudentDetailDto> ExecuteAsync(
        UpdateStudentCommand command,
        CancellationToken cancellationToken)
    {
        var student = await students.FindAsync(command.StudentId, tracked: true, cancellationToken);
        student.Rename(command.Student.FullName, timeProvider.GetUtcNow());
        EnsureSucceeded(await userManager.UpdateAsync(student));
        return await studentQueries.ExecuteAsync(new GetStudentQuery(command.StudentId), cancellationToken);
    }

    public async Task ExecuteAsync(DisableStudentCommand command, CancellationToken cancellationToken)
    {
        var student = await students.FindAsync(command.StudentId, tracked: true, cancellationToken);
        student.Disable(timeProvider.GetUtcNow());
        EnsureSucceeded(await userManager.UpdateAsync(student));
        EnsureSucceeded(await userManager.UpdateSecurityStampAsync(student));
        await sessionRevoker.RevokeAllForUserAsync(
            student.Id,
            SessionRevocationReason.AccountDisabled,
            cancellationToken);
        auditLogger.AccountDisabled(command.ActorUserId, student.Id);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new RequestValidationException(
                "Student account validation failed",
                string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
