using Elearning.Domain;
using Elearning.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elearning.Infrastructure.Persistence.Configurations;

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments", table => table.HasCheckConstraint(
            "CK_Enrollments_Status",
            "\"Status\" IN ('Active', 'Inactive')"));
        builder.HasKey(enrollment => enrollment.Id);
        builder.Property(enrollment => enrollment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasOne(enrollment => enrollment.Course)
            .WithMany(course => course.Enrollments)
            .HasForeignKey(enrollment => enrollment.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(enrollment => enrollment.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(enrollment => new { enrollment.StudentId, enrollment.CourseId }).IsUnique();
        builder.HasIndex(enrollment => new { enrollment.CourseId, enrollment.Status });
        builder.HasIndex(enrollment => new { enrollment.StudentId, enrollment.Status });
    }
}
