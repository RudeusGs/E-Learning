using Elearning.Domain;
using Elearning.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elearning.Infrastructure.Persistence.Configurations;

public sealed class StudentAnswerConfiguration : IEntityTypeConfiguration<StudentAnswer>
{
    public void Configure(EntityTypeBuilder<StudentAnswer> builder)
    {
        builder.ToTable("StudentAnswers");
        builder.HasKey(answer => answer.Id);
        builder.HasOne(answer => answer.Question)
            .WithMany()
            .HasForeignKey(answer => answer.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(answer => answer.Option)
            .WithMany()
            .HasForeignKey(answer => new { answer.QuestionId, answer.OptionId })
            .HasPrincipalKey(option => new { option.QuestionId, option.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(answer => answer.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(answer => new { answer.StudentId, answer.QuestionId, answer.AnsweredAtUtc });
        builder.HasIndex(answer => new { answer.QuestionId, answer.AnsweredAtUtc });
    }
}
