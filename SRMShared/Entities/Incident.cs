namespace SRMShared.Entities;

public class Incident : BaseEntity
{
    public Guid ServerRoomId { get; set; }
    public Guid? ShellyDeviceId { get; set; }
    public Guid? MonitoredDeviceId { get; set; }
    public IncidentType Type { get; set; }
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public string CorrelationKey { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime? LastOccurredAtUtc { get; set; }

    public ServerRoom? ServerRoom { get; set; }
    public ShellyDevice? ShellyDevice { get; set; }
    public MonitoredDevice? MonitoredDevice { get; set; }
    public ICollection<IncidentEvent> Events { get; set; } = new List<IncidentEvent>();
    public ICollection<TicketLink> TicketLinks { get; set; } = new List<TicketLink>();
}
