using Elearning.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elearning.Infrastructure.Persistence.Configurations;

public sealed class BlacklistedTokenConfiguration : IEntityTypeConfiguration<BlacklistedToken>
{
    public void Configure(EntityTypeBuilder<BlacklistedToken> builder)
    {
        builder.ToTable("BlacklistedTokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenId).HasMaxLength(64).IsRequired();
        builder.Property(token => token.Reason).HasMaxLength(100).IsRequired();

        builder.HasIndex(token => token.TokenId).IsUnique();
        builder.HasIndex(token => token.ExpiresAtUtc);
        builder.HasIndex(token => token.UserId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
