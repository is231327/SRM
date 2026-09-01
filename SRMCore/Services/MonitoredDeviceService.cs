using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class MonitoredDeviceService(SrmCoreDbContext dbContext, ICurrentUserContext currentUserContext) : CrudService<MonitoredDevice>(dbContext, currentUserContext), IMonitoredDeviceService
{
    public override async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await ApplyOwnershipFilter(DbContext.MonitoredDevices)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return false;
        }

        var incidents = await DbContext.Incidents
            .Where(x => x.MonitoredDeviceId == id)
            .ToListAsync();
        foreach (var incident in incidents)
        {
            incident.MonitoredDeviceId = null;
        }

        DbContext.MonitoredDevices.Remove(existing);
        await DbContext.SaveChangesAsync();
        return true;
    }
}
