using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class MaintenanceWindowService(SrmCoreDbContext dbContext, ICurrentUserContext currentUserContext) : CrudService<MaintenanceWindow>(dbContext, currentUserContext), IMaintenanceWindowService
{
}
