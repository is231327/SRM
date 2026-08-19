using System.ComponentModel.DataAnnotations;

namespace SRMShared.DTOs.Auth;

public class AgentCredentialCreateRequestDto : AgentCredentialBaseDto
{
    [Required]
    [StringLength(200)]
    public string ClientSecret { get; set; } = string.Empty;
}
