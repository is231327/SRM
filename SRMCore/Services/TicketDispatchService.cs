using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class TicketDispatchService(SrmCoreDbContext dbContext) : ITicketDispatchService
{
    private const string ProviderName = "Redmine";

    public async Task QueueCreateAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.TicketLinks
            .FirstOrDefaultAsync(x => x.IncidentId == incident.Id && x.ProviderName == ProviderName, cancellationToken);

        if (existing is null)
        {
            dbContext.TicketLinks.Add(new TicketLink
            {
                IncidentId = incident.Id,
                ProviderName = ProviderName,
                SyncStatus = TicketSyncStatus.PendingCreate,
                LastSyncAttemptAtUtc = null,
                SyncAttemptCount = 0,
                NextSyncAttemptAtUtc = null,
                PendingComment = string.Empty,
                LastErrorMessage = string.Empty
            });
        }
        else if (existing.SyncStatus == TicketSyncStatus.Error
            && string.IsNullOrWhiteSpace(existing.ExternalTicketId))
        {
            existing.SyncStatus = TicketSyncStatus.PendingCreate;
            existing.LastSyncAttemptAtUtc = null;
            existing.SyncAttemptCount = 0;
            existing.NextSyncAttemptAtUtc = null;
            existing.PendingComment = string.Empty;
            existing.LastErrorMessage = string.Empty;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task QueueResolutionCommentAsync(Incident incident, string comment, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.TicketLinks
            .FirstOrDefaultAsync(x => x.IncidentId == incident.Id && x.ProviderName == ProviderName, cancellationToken);

        if (existing is null)
        {
            dbContext.TicketLinks.Add(new TicketLink
            {
                IncidentId = incident.Id,
                ProviderName = ProviderName,
                SyncStatus = TicketSyncStatus.PendingCreate,
                LastSyncAttemptAtUtc = null,
                SyncAttemptCount = 0,
                NextSyncAttemptAtUtc = null,
                PendingComment = comment,
                LastErrorMessage = string.Empty
            });
        }
        else
        {
            existing.SyncStatus = string.IsNullOrWhiteSpace(existing.ExternalTicketId)
                ? TicketSyncStatus.PendingCreate
                : TicketSyncStatus.Created;
            existing.LastSyncAttemptAtUtc = null;
            existing.SyncAttemptCount = 0;
            existing.NextSyncAttemptAtUtc = null;
            existing.PendingComment = comment;
            existing.LastErrorMessage = string.Empty;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task QueuePriorityUpdateAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.TicketLinks
            .FirstOrDefaultAsync(x => x.IncidentId == incident.Id && x.ProviderName == ProviderName, cancellationToken);

        if (existing is null || string.IsNullOrWhiteSpace(existing.ExternalTicketId))
        {
            await QueueCreateAsync(incident, cancellationToken);
            return;
        }

        existing.PriorityUpdatePending = true;
        existing.NextSyncAttemptAtUtc = null;
        existing.SyncAttemptCount = 0;
        existing.LastErrorMessage = string.Empty;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
