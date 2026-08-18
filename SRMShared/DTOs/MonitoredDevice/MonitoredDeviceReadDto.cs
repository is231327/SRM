namespace SRMShared.DTOs.MonitoredDevice;

public class MonitoredDeviceReadDto : MonitoredDeviceBaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
