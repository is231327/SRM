using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class AgentService(SrmCoreDbContext dbContext) : CrudService<Agent>(dbContext), IAgentService
{
}
