using System.ComponentModel.DataAnnotations;

namespace SRMShared.DTOs.Auth;

public class LoginRequestDto
{
    [Required]
    [StringLength(200)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Password { get; set; } = string.Empty;
}
