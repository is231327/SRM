using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class ServerRoomService(SrmCoreDbContext dbContext) : CrudService<ServerRoom>(dbContext), IServerRoomService
{
}
