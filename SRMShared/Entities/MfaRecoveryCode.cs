namespace SRMShared.Entities;

public class MfaRecoveryCode : BaseEntity
{
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime? UsedAtUtc { get; set; }

    public AuthUser? User { get; set; }
}
