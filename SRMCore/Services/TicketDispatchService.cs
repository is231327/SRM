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
                LastErrorMessage = string.Empty
            });
        }
        else
        {
            existing.SyncStatus = TicketSyncStatus.PendingCreate;
            existing.LastSyncAttemptAtUtc = null;
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
                LastErrorMessage = comment
            });
        }
        else
        {
            existing.SyncStatus = string.IsNullOrWhiteSpace(existing.ExternalTicketId)
                ? TicketSyncStatus.PendingCreate
                : TicketSyncStatus.PendingComment;
            existing.LastSyncAttemptAtUtc = null;
            existing.LastErrorMessage = comment;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
