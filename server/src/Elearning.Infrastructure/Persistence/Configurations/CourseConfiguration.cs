using Elearning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elearning.Infrastructure.Persistence.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses", table => table.HasCheckConstraint(
            "CK_Courses_Status",
            "\"Status\" IN ('Draft', 'Published', 'Archived')"));
        builder.HasKey(course => course.Id);
        builder.Property(course => course.Title).HasMaxLength(200).IsRequired();
        builder.Property(course => course.Description).HasMaxLength(4000);
        builder.Property(course => course.ThumbnailUrl).HasMaxLength(2048);
        builder.Property(course => course.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(course => course.Version).IsConcurrencyToken();
        builder.HasIndex(course => new { course.Status, course.SortOrder });
        builder.HasIndex(course => course.SortOrder);
        builder.HasIndex(course => course.Title)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
