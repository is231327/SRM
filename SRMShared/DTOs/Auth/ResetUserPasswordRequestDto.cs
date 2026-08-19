using System.ComponentModel.DataAnnotations;

namespace SRMShared.DTOs.Auth;

public class ResetUserPasswordRequestDto
{
    [Required]
    [StringLength(200)]
    public string NewPassword { get; set; } = string.Empty;
}
