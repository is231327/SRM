using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class ShellyDeviceService(SrmCoreDbContext dbContext) : CrudService<ShellyDevice>(dbContext), IShellyDeviceService
{
}
