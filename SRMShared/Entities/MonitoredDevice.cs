namespace SRMShared.Entities;

public class MonitoredDevice : BaseEntity
{
    public Guid AgentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; }
    public int TimeoutMilliseconds { get; set; }
    public int FailureThreshold { get; set; }
    public bool IsActive { get; set; }

    public Agent? Agent { get; set; }
    public ICollection<MonitoredDevicePingResult> PingResults { get; set; } = new List<MonitoredDevicePingResult>();
}
