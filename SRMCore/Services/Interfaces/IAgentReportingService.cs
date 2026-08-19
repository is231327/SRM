using SRMShared.DTOs.AgentReporting;
using SRMShared.Entities;

namespace SRMCore.Services.Interfaces;

public interface IAgentReportingService
{
    Task<SensorReading> CreateSensorReadingAsync(AgentSensorReadingReportDto dto);
    Task<MonitoredDevicePingResult> CreatePingResultAsync(AgentPingResultReportDto dto);
}
