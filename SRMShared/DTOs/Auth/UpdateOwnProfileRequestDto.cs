using System.ComponentModel.DataAnnotations;

namespace SRMShared.DTOs.Auth;

public class UpdateOwnProfileRequestDto
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    public string PhoneNumber { get; set; } = string.Empty;
}
