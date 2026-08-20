namespace SRMShared.Entities;

public class IncidentEvent : BaseEntity
{
    public Guid IncidentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public Incident? Incident { get; set; }
}
