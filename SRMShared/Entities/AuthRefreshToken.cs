namespace SRMShared.Entities;

public class AuthRefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string ReplacedByTokenHash { get; set; } = string.Empty;

    public AuthUser? User { get; set; }
}
