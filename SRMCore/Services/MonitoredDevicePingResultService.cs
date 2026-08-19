using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class MonitoredDevicePingResultService(
    SrmCoreDbContext dbContext,
    ICurrentUserContext currentUserContext) : CrudService<MonitoredDevicePingResult>(dbContext, currentUserContext), IMonitoredDevicePingResultService
{
}
