using Elearning.Domain;
using Microsoft.AspNetCore.Identity;

namespace Elearning.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<long>
{
    public string FullName { get; set; } = string.Empty;
    public AccountStatus Status { get; private set; } = AccountStatus.Active;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Rename(string fullName, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainValidationException("Student full name is required.");
        }

        if (fullName.Trim().Length > 200)
        {
            throw new DomainValidationException("Student full name cannot exceed 200 characters.");
        }

        FullName = fullName.Trim();
        UpdatedAtUtc = now;
    }

    public void Disable(DateTimeOffset now)
    {
        Status = AccountStatus.Disabled;
        UpdatedAtUtc = now;
    }
}
