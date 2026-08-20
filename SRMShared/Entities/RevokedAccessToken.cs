namespace SRMShared.Entities;

public class RevokedAccessToken : BaseEntity
{
    public string TokenJti { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public Guid? UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
