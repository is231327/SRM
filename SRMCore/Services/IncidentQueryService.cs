using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class IncidentQueryService(
    SrmCoreDbContext dbContext,
    ICurrentUserContext currentUserContext) : IIncidentQueryService
{
    private static readonly string[] HiddenTicketStatuses = ["Resolved", "Rejected", "Closed"];

    public async Task<List<Incident>> GetAllAsync(bool includeClosed = false, CancellationToken cancellationToken = default)
    {
        var query = includeClosed ? dbContext.Incidents : ApplyVisibleTicketFilter(dbContext.Incidents);
        return await ApplyOwnershipFilter(query)
            .Include(x => x.ServerRoom)
            .Include(x => x.ShellyDevice)
            .Include(x => x.MonitoredDevice)
            .Include(x => x.Events)
            .Include(x => x.TicketLinks)
            .AsSplitQuery()
            .OrderByDescending(x => x.OpenedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ApplyOwnershipFilter(ApplyVisibleTicketFilter(dbContext.Incidents))
            .Include(x => x.ServerRoom)
            .Include(x => x.ShellyDevice)
            .Include(x => x.MonitoredDevice)
            .Include(x => x.Events)
            .Include(x => x.TicketLinks)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private static IQueryable<Incident> ApplyVisibleTicketFilter(IQueryable<Incident> query)
    {
        return query.Where(x => !x.TicketLinks.Any(
            ticketLink => HiddenTicketStatuses.Contains(ticketLink.ExternalStatusName)));
    }

    private IQueryable<Incident> ApplyOwnershipFilter(IQueryable<Incident> query)
    {
        if (!currentUserContext.IsCustomerScopedUser)
        {
            return query;
        }

        var customerId = currentUserContext.CustomerId
            ?? throw new ForbiddenAccessException("Customer-scoped users require a customer claim.");

        return query.Where(x => x.ServerRoom != null && x.ServerRoom.CustomerId == customerId);
    }
}
