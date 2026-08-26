using Elearning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elearning.Infrastructure.Persistence.Configurations;

public sealed class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.ToTable("QuestionOptions");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.Content).HasMaxLength(1000).IsRequired();
        builder.HasAlternateKey(option => new { option.QuestionId, option.Id });
        builder.HasIndex(option => new { option.QuestionId, option.SortOrder }).IsUnique();
        builder.HasIndex(option => option.QuestionId)
            .IsUnique()
            .HasFilter("\"IsCorrect\" = TRUE")
            .HasDatabaseName("UX_QuestionOptions_OneCorrect");
    }
}
