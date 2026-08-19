using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class ShellyDeviceService(SrmCoreDbContext dbContext, ICurrentUserContext currentUserContext) : CrudService<ShellyDevice>(dbContext, currentUserContext), IShellyDeviceService
{
}
