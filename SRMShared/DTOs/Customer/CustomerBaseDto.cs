using System.ComponentModel.DataAnnotations;

namespace SRMShared.DTOs.Customer;

public class CustomerBaseDto
{
    [Required]
    [StringLength(100)]
    public string ExternalReference { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ContactPhone { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
