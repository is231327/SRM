using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class MonitoredDeviceService(SrmCoreDbContext dbContext) : CrudService<MonitoredDevice>(dbContext), IMonitoredDeviceService
{
}
