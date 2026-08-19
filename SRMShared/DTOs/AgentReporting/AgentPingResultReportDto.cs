using System.ComponentModel.DataAnnotations;
using SRMShared.Attributes;

namespace SRMShared.DTOs.AgentReporting;

public class AgentPingResultReportDto
{
    [NonEmptyGuid]
    public Guid MonitoredDeviceId { get; set; }

    public bool IsReachable { get; set; }

    [Range(0, long.MaxValue)]
    public long RoundtripTimeMilliseconds { get; set; }

    [Range(0, int.MaxValue)]
    public int ConsecutiveFailureCount { get; set; }

    public bool FailureThresholdReached { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public DateTime RecordedAtUtc { get; set; }
}
