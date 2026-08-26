using Elearning.Domain;
using Elearning.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elearning.Infrastructure.Persistence.Configurations;

public sealed class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.ToTable("LessonProgress", table => table.HasCheckConstraint(
            "CK_LessonProgress_Status",
            "\"Status\" IN ('InProgress', 'Completed')"));
        builder.HasKey(progress => progress.Id);
        builder.Property(progress => progress.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(progress => progress.Version).IsConcurrencyToken();
        builder.HasOne(progress => progress.Lesson)
            .WithMany(lesson => lesson.Progress)
            .HasForeignKey(progress => progress.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(progress => progress.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(progress => new { progress.StudentId, progress.LessonId }).IsUnique();
        builder.HasIndex(progress => new { progress.LessonId, progress.Status });
        builder.HasIndex(progress => new { progress.StudentId, progress.Status });
    }
}
