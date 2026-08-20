namespace SRMShared.Entities;

public enum TicketSyncStatus
{
    PendingCreate = 1,
    Created = 2,
    PendingComment = 3,
    Commented = 4,
    Failed = 5
}
