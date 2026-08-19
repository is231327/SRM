namespace SRMShared.DTOs.MonitoredDevicePingResult;

public class MonitoredDevicePingResultReadDto : MonitoredDevicePingResultBaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
