using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class AgentService(SrmCoreDbContext dbContext, ICurrentUserContext currentUserContext) : CrudService<Agent>(dbContext, currentUserContext), IAgentService
{
}
