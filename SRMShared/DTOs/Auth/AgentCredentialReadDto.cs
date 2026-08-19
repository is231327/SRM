namespace SRMShared.DTOs.Auth;

public class AgentCredentialReadDto : AgentCredentialBaseDto
{
    public Guid Id { get; set; }
    public DateTime? LastAuthenticatedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
