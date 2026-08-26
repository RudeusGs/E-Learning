using Elearning.Domain;
using Elearning.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elearning.Infrastructure.Persistence.Configurations;

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<IdentityRole<long>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<long>> builder)
    {
        var roles = Enum.GetValues<UserRole>()
            .Select(role => new IdentityRole<long>
            {
                Id = (long)role + 1,
                Name = role.ToIdentityName(),
                NormalizedName = role.ToIdentityName().ToUpperInvariant(),
                ConcurrencyStamp = $"structural-role-{role.ToIdentityName().ToLowerInvariant()}-v1"
            });

        builder.HasData(roles);
    }
}
