using System.ComponentModel.DataAnnotations;
using SRMShared.Attributes;

namespace SRMShared.DTOs.Agent;

public class AgentBaseDto
{
    [NonEmptyGuid]
    public Guid ServerRoomId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ApiKeyReference { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Version { get; set; } = string.Empty;

    [Required]
    [HostOrIpAddress]
    public string LastKnownIpAddress { get; set; } = string.Empty;

    public DateTime? LastSeenAtUtc { get; set; }

    public bool IsActive { get; set; }
}
