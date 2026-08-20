using SRMShared.Entities;

namespace SRMShared.DTOs.Incident;

public class TicketLinkReadDto
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ExternalTicketId { get; set; } = string.Empty;
    public string ExternalTicketUrl { get; set; } = string.Empty;
    public TicketSyncStatus SyncStatus { get; set; }
    public string LastErrorMessage { get; set; } = string.Empty;
    public DateTime? LastSyncAttemptAtUtc { get; set; }
    public DateTime? CreatedInExternalSystemAtUtc { get; set; }
    public DateTime? LastCommentedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
