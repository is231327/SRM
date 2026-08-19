using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SRMAgent.Configuration;
using SRMAgent.Services.Interfaces;
using SRMShared.DTOs.Auth;

namespace SRMAgent.Services;

public class AgentAuthApiClient(
    HttpClient httpClient,
    IOptions<AgentApiOptions> options) : IAgentAuthApiClient
{
    public async Task<string?> LoginAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/auth/agent/login",
            new AgentLoginRequestDto
            {
                ClientIdentifier = options.Value.ClientIdentifier,
                ClientSecret = options.Value.ClientSecret
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>(cancellationToken);
        return result?.AccessToken;
    }
}
