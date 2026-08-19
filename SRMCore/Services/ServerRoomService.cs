using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class ServerRoomService(SrmCoreDbContext dbContext, ICurrentUserContext currentUserContext) : CrudService<ServerRoom>(dbContext, currentUserContext), IServerRoomService
{
}
