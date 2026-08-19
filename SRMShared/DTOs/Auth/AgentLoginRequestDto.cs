using System.ComponentModel.DataAnnotations;

namespace SRMShared.DTOs.Auth;

public class AgentLoginRequestDto
{
    [Required]
    [StringLength(200)]
    public string ClientIdentifier { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ClientSecret { get; set; } = string.Empty;
}
