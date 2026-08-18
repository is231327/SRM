using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class SensorReadingService(SrmCoreDbContext dbContext) : CrudService<SensorReading>(dbContext), ISensorReadingService
{
}
