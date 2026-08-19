using SRMShared.DTOs.SensorReading;

namespace SRMAgent.Models.Monitoring;

public class AgentMonitoringCycleResult
{
    public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    public List<SensorReadingReadDto> SubmittedSensorReadings { get; set; } = [];
    public List<MonitoredDevicePingResult> PingResults { get; set; } = [];
}
