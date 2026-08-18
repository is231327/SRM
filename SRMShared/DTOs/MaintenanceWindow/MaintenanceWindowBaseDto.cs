using System.ComponentModel.DataAnnotations;
using SRMShared.Attributes;

namespace SRMShared.DTOs.MaintenanceWindow;

public class MaintenanceWindowBaseDto : IValidatableObject
{
    [NonEmptyGuid]
    public Guid ServerRoomId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndUtc <= StartUtc)
        {
            yield return new ValidationResult(
                "EndUtc must be later than StartUtc.",
                [nameof(EndUtc), nameof(StartUtc)]);
        }
    }
}
