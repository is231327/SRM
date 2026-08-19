using System.ComponentModel.DataAnnotations;

namespace SRMShared.DTOs.Auth;

public class ChangePasswordRequestDto
{
    [Required]
    [StringLength(200)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string NewPassword { get; set; } = string.Empty;
}
