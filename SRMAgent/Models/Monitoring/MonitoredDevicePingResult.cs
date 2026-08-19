namespace SRMAgent.Models.Monitoring;

public class MonitoredDevicePingResult
{
    public Guid MonitoredDeviceId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool IsReachable { get; set; }
    public long RoundtripTimeMilliseconds { get; set; }
    public int ConsecutiveFailureCount { get; set; }
    public bool FailureThresholdReached { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
