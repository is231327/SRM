namespace SRMShared.Entities;

public class SecurityAuditRecord : BaseEntity
{
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string ActorIdentifier { get; set; } = string.Empty;
    public string SourceAddress { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public Guid? CustomerId { get; set; }
    public string Description { get; set; } = string.Empty;
}
