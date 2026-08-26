using Elearning.Application.Exceptions;
using Elearning.Domain;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elearning.Infrastructure.Authorization;

public sealed class ActiveStudentPolicy(ElearningDbContext dbContext)
{
    public async Task EnsureSatisfiedAsync(long studentId, CancellationToken cancellationToken)
    {
        var isActive = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == studentId && user.Status == AccountStatus.Active,
                cancellationToken);

        if (!isActive)
        {
            throw AuthorizationException.AccountDisabled();
        }
    }
}
