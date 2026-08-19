using System.ComponentModel.DataAnnotations;

namespace SRMShared.DTOs.Auth;

public class AgentCredentialBaseDto
{
    public Guid AgentId { get; set; }

    [Required]
    [StringLength(200)]
    public string ClientIdentifier { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
