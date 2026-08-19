using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SRMAgent.Configuration;
using SRMAgent.Services.Interfaces;

namespace SRMAgent.Services;

public class AgentAuthApiClient(
    HttpClient httpClient,
    IOptions<AgentApiOptions> options) : IAgentAuthApiClient
{
    public async Task<string?> LoginAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
