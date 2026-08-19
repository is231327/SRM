namespace SRMAgent;

public class AgentData
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public DateTime? LastConfigurationRefreshAtUtc { get; set; }
    public DateTime? LastMonitoringCycleAtUtc { get; set; }
    public int ShellyDeviceCount { get; set; }
    public int MonitoredDeviceCount { get; set; }
    public int SubmittedSensorReadingCount { get; set; }
    public int ReachableMonitoredDeviceCount { get; set; }
    public int UnreachableMonitoredDeviceCount { get; set; }
}
