using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.AgentReporting;
using SRMShared.DTOs.MonitoredDevicePingResult;
using SRMShared.DTOs.SensorReading;
using SRMShared.Entities;

namespace SRMCore.Controllers;

[ApiController]
[Route("api/agent-reporting")]
[Authorize(Roles = "Agent")]
public class AgentReportingController(
    IAgentReportingService agentReportingService,
    ICrudDtoMapper<SensorReading, SensorReadingCreateDto, SensorReadingUpdateDto, SensorReadingReadDto> sensorReadingMapper,
    ICrudDtoMapper<MonitoredDevicePingResult, MonitoredDevicePingResultCreateDto, MonitoredDevicePingResultUpdateDto, MonitoredDevicePingResultReadDto> pingResultMapper) : ControllerBase
{
    [HttpPost("sensor-readings")]
    public async Task<ActionResult<SensorReadingReadDto>> CreateSensorReading(AgentSensorReadingReportDto dto)
    {
        var sensorReading = await agentReportingService.CreateSensorReadingAsync(dto);
        return Ok(sensorReadingMapper.ToReadDto(sensorReading));
    }

    [HttpPost("ping-results")]
    public async Task<ActionResult<MonitoredDevicePingResultReadDto>> CreatePingResult(AgentPingResultReportDto dto)
    {
        var pingResult = await agentReportingService.CreatePingResultAsync(dto);
        return Ok(pingResultMapper.ToReadDto(pingResult));
    }
}
