using SRMShared.DTOs.AgentRuntime;

namespace SRMCore.Services.Interfaces;

public interface IAgentRuntimeService
{
    Task<AgentRuntimeConfigurationDto?> GetRuntimeConfigurationAsync();
}
