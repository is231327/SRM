using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SRMAgent.Configuration;
using SRMAgent.Models.Shelly;
using SRMAgent.Services.Interfaces;

namespace SRMAgent.Services;

public class VirtualShellyClient(
    HttpClient httpClient,
    IOptions<AgentRuntimeOptions> runtimeOptions) : IVirtualShellyClient
{
    public async Task<VirtualShellyStatusResponse?> GetStatusAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var statusPath = runtimeOptions.Value.ShellyStatusPath.TrimStart('/');
        var requestUri = $"{normalizedBaseUrl}/{statusPath}";

        return await httpClient.GetFromJsonAsync<VirtualShellyStatusResponse>(requestUri, cancellationToken);
    }
}
