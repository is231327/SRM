namespace SRMShared.Auth;

public class RevokedAccessTokenState
{
    public Guid UserId { get; set; }
    public string TokenJti { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
