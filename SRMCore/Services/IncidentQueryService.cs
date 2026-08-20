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
    public async Task<List<Incident>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await ApplyOwnershipFilter(dbContext.Incidents)
            .Include(x => x.ServerRoom)
            .Include(x => x.ShellyDevice)
            .Include(x => x.MonitoredDevice)
            .Include(x => x.Events)
            .Include(x => x.TicketLinks)
            .OrderByDescending(x => x.OpenedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ApplyOwnershipFilter(dbContext.Incidents)
            .Include(x => x.ServerRoom)
            .Include(x => x.ShellyDevice)
            .Include(x => x.MonitoredDevice)
            .Include(x => x.Events)
            .Include(x => x.TicketLinks)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
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
