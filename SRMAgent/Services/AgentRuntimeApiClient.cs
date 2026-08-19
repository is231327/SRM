using System.Net.Http.Headers;
using System.Net.Http.Json;
using SRMAgent.Services.Interfaces;
using SRMShared.DTOs.AgentRuntime;

namespace SRMAgent.Services;

public class AgentRuntimeApiClient(HttpClient httpClient) : IAgentRuntimeApiClient
{
    public async Task<AgentRuntimeConfigurationDto?> GetConfigurationAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await httpClient.GetFromJsonAsync<AgentRuntimeConfigurationDto>(
            "api/agent-runtime/configuration",
            cancellationToken);
    }
}
