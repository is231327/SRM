using SRMShared.DTOs.AgentReporting;
using SRMShared.DTOs.MonitoredDevicePingResult;
using SRMShared.DTOs.SensorReading;

namespace SRMAgent.Services.Interfaces;

public interface IAgentCoreApiClient
{
    Task<SensorReadingReadDto?> SubmitSensorReadingAsync(
        string accessToken,
        AgentSensorReadingReportDto dto,
        CancellationToken cancellationToken = default);

    Task<MonitoredDevicePingResultReadDto?> SubmitPingResultAsync(
        string accessToken,
        AgentPingResultReportDto dto,
        CancellationToken cancellationToken = default);
}
