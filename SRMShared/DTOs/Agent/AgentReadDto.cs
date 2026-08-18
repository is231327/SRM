namespace SRMShared.DTOs.Agent;

public class AgentReadDto : AgentBaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
