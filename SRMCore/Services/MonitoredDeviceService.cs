using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class MonitoredDeviceService(SrmCoreDbContext dbContext, ICurrentUserContext currentUserContext) : CrudService<MonitoredDevice>(dbContext, currentUserContext), IMonitoredDeviceService
{
}
