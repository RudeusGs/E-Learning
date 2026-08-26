using Elearning.Application.Exceptions;
using Elearning.Domain;
using Elearning.Infrastructure.Identity;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Students;

public sealed class StudentAccountStore(ElearningDbContext dbContext)
{
    private static readonly string StudentRoleName = UserRole.Student.ToIdentityName();

    public IQueryable<ApplicationUser> Query(bool tracked)
    {
        var users = tracked ? dbContext.Users.AsQueryable() : dbContext.Users.AsNoTracking();
        return users.Where(user => dbContext.UserRoles.Any(userRole =>
            userRole.UserId == user.Id &&
            dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Name == StudentRoleName)));
    }

    public async Task<ApplicationUser> FindAsync(
        long id,
        bool tracked,
        CancellationToken cancellationToken) =>
        await Query(tracked).SingleOrDefaultAsync(user => user.Id == id, cancellationToken)
        ?? throw new ResourceNotFoundException("Student");
}
