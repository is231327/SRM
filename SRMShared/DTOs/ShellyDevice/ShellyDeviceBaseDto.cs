using System.ComponentModel.DataAnnotations;
using SRMShared.Validation;

namespace SRMShared.DTOs.ShellyDevice;

public class ShellyDeviceBaseDto
{
    [NonEmptyGuid]
    public Guid AgentId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DeviceType { get; set; } = string.Empty;

    [Required]
    [Url]
    [StringLength(500)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$", ErrorMessage = "The field must contain a valid MAC address.")]
    public string MacAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirmwareVersion { get; set; } = string.Empty;

    public bool IsVirtual { get; set; }

    public bool IsActive { get; set; }
}
