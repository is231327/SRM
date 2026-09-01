namespace SRMShared.Entities;

public class TicketLink : BaseEntity
{
    public Guid IncidentId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ExternalTicketId { get; set; } = string.Empty;
    public string ExternalTicketUrl { get; set; } = string.Empty;
    public string ExternalStatusName { get; set; } = string.Empty;
    public string ExternalPriorityName { get; set; } = string.Empty;
    public DateTime? ExternalDataSynchronizedAtUtc { get; set; }
    public TicketSyncStatus SyncStatus { get; set; } = TicketSyncStatus.PendingCreate;
    public string LastErrorMessage { get; set; } = string.Empty;
    public string PendingComment { get; set; } = string.Empty;
    public bool PriorityUpdatePending { get; set; }
    public DateTime? LastSyncAttemptAtUtc { get; set; }
    public int SyncAttemptCount { get; set; }
    public DateTime? NextSyncAttemptAtUtc { get; set; }
    public DateTime? CreatedInExternalSystemAtUtc { get; set; }
    public DateTime? LastCommentedAtUtc { get; set; }

    public Incident? Incident { get; set; }
}
