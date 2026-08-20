namespace SRMShared.Entities;

public class TicketLink : BaseEntity
{
    public Guid IncidentId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ExternalTicketId { get; set; } = string.Empty;
    public string ExternalTicketUrl { get; set; } = string.Empty;
    public TicketSyncStatus SyncStatus { get; set; } = TicketSyncStatus.PendingCreate;
    public string LastErrorMessage { get; set; } = string.Empty;
    public DateTime? LastSyncAttemptAtUtc { get; set; }
    public DateTime? CreatedInExternalSystemAtUtc { get; set; }
    public DateTime? LastCommentedAtUtc { get; set; }

    public Incident? Incident { get; set; }
}
