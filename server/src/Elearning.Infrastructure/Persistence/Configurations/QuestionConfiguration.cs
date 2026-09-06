
using Elearning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elearning.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions", table =>
        {
            table.HasCheckConstraint(
                "CK_Questions_Type",
                "\"Type\" IN ('MultipleChoice', 'TrueFalse')");
            table.HasCheckConstraint(
                "CK_Questions_Placement",
                "\"Placement\" IN ('Reinforcement', 'VideoCheckpoint')");
            table.HasCheckConstraint(
                "CK_Questions_VideoTimestamp",
                "(\"Placement\" = 'Reinforcement' AND \"VideoTimestampSeconds\" IS NULL) OR " +
                "(\"Placement\" = 'VideoCheckpoint' AND \"VideoTimestampSeconds\" >= 1)");
        });
        builder.HasKey(question => question.Id);
        builder.Property(question => question.Text).IsRequired();
        builder.Property(question => question.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(question => question.Placement).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(question => question.VideoTimestampSeconds);
        builder.Property(question => question.Explanation).HasMaxLength(4000);
        builder.Property(question => question.Version).IsConcurrencyToken();
        builder.HasOne(question => question.Lesson)
            .WithMany(lesson => lesson.Questions)
            .HasForeignKey(question => question.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(question => question.Options)
            .WithOne(option => option.Question)
            .HasForeignKey(option => option.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(question => question.Options).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(question => new { question.LessonId, question.SortOrder }).IsUnique();
        builder.HasIndex(question => new
        {
            question.LessonId,
            question.Placement,
            question.VideoTimestampSeconds
        });
    }
}
