using System.Net.Http.Headers;
using System.Net.Http.Json;
using SRMAgent.Services.Interfaces;
using SRMShared.DTOs.AgentReporting;
using SRMShared.DTOs.MonitoredDevicePingResult;
using SRMShared.DTOs.SensorReading;

namespace SRMAgent.Services;

public class AgentCoreApiClient(HttpClient httpClient) : IAgentCoreApiClient
{
    public async Task<SensorReadingReadDto?> SubmitSensorReadingAsync(
        string accessToken,
        AgentSensorReadingReportDto dto,
        CancellationToken cancellationToken = default)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await httpClient.PostAsJsonAsync("api/agent-reporting/sensor-readings", dto, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SensorReadingReadDto>(cancellationToken);
    }

    public async Task<MonitoredDevicePingResultReadDto?> SubmitPingResultAsync(
        string accessToken,
        AgentPingResultReportDto dto,
        CancellationToken cancellationToken = default)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await httpClient.PostAsJsonAsync("api/agent-reporting/ping-results", dto, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MonitoredDevicePingResultReadDto>(cancellationToken);
    }
}
