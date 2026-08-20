namespace SRMShared.DTOs.Incident;

public class IncidentEventReadDto
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
