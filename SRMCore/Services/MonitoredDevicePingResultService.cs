using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class MonitoredDevicePingResultService(
    SrmCoreDbContext dbContext) : CrudService<MonitoredDevicePingResult>(dbContext), IMonitoredDevicePingResultService
{
}
