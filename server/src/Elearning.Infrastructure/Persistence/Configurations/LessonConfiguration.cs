
using Elearning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elearning.Infrastructure.Persistence.Configurations;

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons", table =>
        {
            table.HasCheckConstraint(
                "CK_Lessons_Status",
                "\"Status\" IN ('Draft', 'Published', 'Archived')");
            table.HasCheckConstraint(
                "CK_Lessons_VideoDuration",
                "\"VideoDurationSeconds\" IS NULL OR \"VideoDurationSeconds\" BETWEEN 1 AND 43200");
        });
        builder.HasKey(lesson => lesson.Id);
        builder.Property(lesson => lesson.Title).HasMaxLength(200).IsRequired();
        builder.Property(lesson => lesson.Description).HasMaxLength(4000);
        builder.Property(lesson => lesson.ContentHtml);
        builder.Property(lesson => lesson.VideoProvider).HasConversion<string>().HasMaxLength(20);
        builder.Property(lesson => lesson.VideoExternalId).HasMaxLength(32);
        builder.Property(lesson => lesson.VideoDurationSeconds);
        builder.Property(lesson => lesson.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(lesson => lesson.Version).IsConcurrencyToken();
        builder.HasOne(lesson => lesson.Course)
            .WithMany(course => course.Lessons)
            .HasForeignKey(lesson => lesson.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(lesson => new { lesson.CourseId, lesson.Status, lesson.SortOrder });
        builder.HasIndex(lesson => lesson.Status);
        builder.HasIndex(lesson => new { lesson.CourseId, lesson.SortOrder })
            .IsUnique()
            .HasFilter("\"Status\" <> 'Archived'");
    }
}
