using System.ComponentModel.DataAnnotations;

namespace SRMShared.DTOs.Auth;

public class AgentCredentialUpdateRequestDto : AgentCredentialBaseDto
{
    [StringLength(200)]
    public string? NewClientSecret { get; set; }
}
