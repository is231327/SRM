using SRMShared.DTOs.AgentRuntime;

namespace SRMAgent.Services.Interfaces;

public interface IAgentRuntimeApiClient
{
    Task<AgentRuntimeConfigurationDto?> GetConfigurationAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}
