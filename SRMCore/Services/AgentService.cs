using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class AgentService(SrmCoreDbContext dbContext, ICurrentUserContext currentUserContext) : CrudService<Agent>(dbContext, currentUserContext), IAgentService
{
    public override async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await ApplyOwnershipFilter(DbContext.Agents)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return false;
        }

        var shellyDeviceIds = await DbContext.ShellyDevices
            .Where(x => x.AgentId == id)
            .Select(x => x.Id)
            .ToListAsync();
        var monitoredDeviceIds = await DbContext.MonitoredDevices
            .Where(x => x.AgentId == id)
            .Select(x => x.Id)
            .ToListAsync();
        var incidents = await DbContext.Incidents
            .Where(x => (x.ShellyDeviceId.HasValue && shellyDeviceIds.Contains(x.ShellyDeviceId.Value))
                || (x.MonitoredDeviceId.HasValue && monitoredDeviceIds.Contains(x.MonitoredDeviceId.Value)))
            .ToListAsync();

        foreach (var incident in incidents)
        {
            if (incident.ShellyDeviceId.HasValue && shellyDeviceIds.Contains(incident.ShellyDeviceId.Value))
            {
                incident.ShellyDeviceId = null;
            }

            if (incident.MonitoredDeviceId.HasValue && monitoredDeviceIds.Contains(incident.MonitoredDeviceId.Value))
            {
                incident.MonitoredDeviceId = null;
            }
        }

        DbContext.Agents.Remove(existing);
        await DbContext.SaveChangesAsync();
        return true;
    }
}
