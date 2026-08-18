using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class MaintenanceWindowService(SrmCoreDbContext dbContext) : CrudService<MaintenanceWindow>(dbContext), IMaintenanceWindowService
{
}
