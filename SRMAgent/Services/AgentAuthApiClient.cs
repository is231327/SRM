using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SRMAgent.Services.Interfaces;
using SRMShared.DTOs.Auth;

namespace SRMAgent.Services;

public class AgentAuthApiClient(
    HttpClient httpClient,
    IConfiguration configuration) : IAgentAuthApiClient
{
    public async Task<string?> LoginAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/auth/agent/login",
            new AgentLoginRequestDto
            {
                ClientIdentifier = configuration["AgentApi:ClientIdentifier"] ?? string.Empty,
                ClientSecret = configuration["AgentApi:ClientSecret"] ?? string.Empty
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
