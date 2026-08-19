namespace SRMShared.Entities;

public class MonitoredDevicePingResult : BaseEntity
{
    public Guid MonitoredDeviceId { get; set; }
    public bool IsReachable { get; set; }
    public long RoundtripTimeMilliseconds { get; set; }
    public int ConsecutiveFailureCount { get; set; }
    public bool FailureThresholdReached { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

    public MonitoredDevice? MonitoredDevice { get; set; }
}
