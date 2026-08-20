using SRMShared.Entities;

namespace SRMShared.DTOs.Incident;

public class IncidentReadDto
{
    public Guid Id { get; set; }
    public Guid ServerRoomId { get; set; }
    public string ServerRoomName { get; set; } = string.Empty;
    public Guid? ShellyDeviceId { get; set; }
    public string ShellyDeviceName { get; set; } = string.Empty;
    public Guid? MonitoredDeviceId { get; set; }
    public string MonitoredDeviceName { get; set; } = string.Empty;
    public IncidentType Type { get; set; }
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }
    public string CorrelationKey { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime? LastOccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<IncidentEventReadDto> Events { get; set; } = [];
    public List<TicketLinkReadDto> TicketLinks { get; set; } = [];
}
