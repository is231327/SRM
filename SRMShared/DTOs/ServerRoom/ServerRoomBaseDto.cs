using System.ComponentModel.DataAnnotations;
using SRMShared.Attributes;

namespace SRMShared.DTOs.ServerRoom;

public class ServerRoomBaseDto : IValidatableObject
{
    [NonEmptyGuid]
    public Guid CustomerId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string LocationDescription { get; set; } = string.Empty;

    [Range(-50, 100)]
    public float TemperatureWarningThreshold { get; set; }

    [Range(-50, 100)]
    public float TemperatureCriticalThreshold { get; set; }

    public bool MonitoringEnabled { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TemperatureCriticalThreshold <= TemperatureWarningThreshold)
        {
            yield return new ValidationResult(
                "TemperatureCriticalThreshold must be greater than TemperatureWarningThreshold.",
                [nameof(TemperatureCriticalThreshold), nameof(TemperatureWarningThreshold)]);
        }
    }
}
