namespace SRMShared.Entities;

public class AgentCredential : BaseEntity
{
    public Guid AgentId { get; set; }
    public string ClientIdentifier { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastAuthenticatedAtUtc { get; set; }
}
