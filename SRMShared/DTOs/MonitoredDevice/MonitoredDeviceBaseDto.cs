using System.ComponentModel.DataAnnotations;
using SRMShared.Attributes;

namespace SRMShared.DTOs.MonitoredDevice;

public class MonitoredDeviceBaseDto
{
    [NonEmptyGuid]
    public Guid AgentId { get; set; }

    [Required]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [IpAddress]
    public string IpAddress { get; set; } = string.Empty;

    [Range(1, 86400)]
    public int IntervalSeconds { get; set; }

    [Range(1, 60000)]
    public int TimeoutMilliseconds { get; set; }

    [Range(1, 100)]
    public int FailureThreshold { get; set; }

    public bool IsActive { get; set; }
}
