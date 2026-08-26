using Elearning.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elearning.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(token => token.SecurityStampHash).HasMaxLength(64).IsRequired();
        builder.Property(token => token.AccessTokenJti).HasMaxLength(64).IsRequired();
        builder.Property(token => token.RevocationReason).HasMaxLength(100);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.AccessTokenJti).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.FamilyId });
        builder.HasIndex(token => token.ExpiresAtUtc);

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
