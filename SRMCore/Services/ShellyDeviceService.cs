using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class ShellyDeviceService(SrmCoreDbContext dbContext, ICurrentUserContext currentUserContext) : CrudService<ShellyDevice>(dbContext, currentUserContext), IShellyDeviceService
{
    public override async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await ApplyOwnershipFilter(DbContext.ShellyDevices)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return false;
        }

        var incidents = await DbContext.Incidents
            .Where(x => x.ShellyDeviceId == id)
            .ToListAsync();
        foreach (var incident in incidents)
        {
            incident.ShellyDeviceId = null;
        }

        DbContext.ShellyDevices.Remove(existing);
        await DbContext.SaveChangesAsync();
        return true;
    }
}
