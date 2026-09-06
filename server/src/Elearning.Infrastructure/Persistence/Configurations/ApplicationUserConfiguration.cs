using Elearning.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elearning.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.FullName).HasMaxLength(200).IsRequired();
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(user => user.CreatedAtUtc).IsRequired();
        builder.Property(user => user.UpdatedAtUtc).IsRequired();
        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("EmailIndex")
            .HasFilter("\"NormalizedEmail\" IS NOT NULL");
        builder.HasIndex(user => user.Status);
        builder.HasIndex(user => user.FullName)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(user => user.Email)
            .HasDatabaseName("IX_AspNetUsers_Email_Trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AspNetUsers_Status",
            "\"Status\" IN ('Active', 'Disabled')"));
    }
}
