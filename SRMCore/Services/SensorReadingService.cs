using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class SensorReadingService(SrmCoreDbContext dbContext, ICurrentUserContext currentUserContext) : CrudService<SensorReading>(dbContext, currentUserContext), ISensorReadingService
{
}
