namespace Elearning.Infrastructure.Identity;

public sealed class BlacklistedToken
{
    public long Id { get; set; }
    public string TokenId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset RevokedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}
