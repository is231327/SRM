using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Security;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.AgentReporting;
using SRMShared.Entities;

namespace SRMCore.Services;

public class AgentReportingService(
    SrmCoreDbContext dbContext,
    ICurrentUserContext currentUserContext) : IAgentReportingService
{
    public async Task<SensorReading> CreateSensorReadingAsync(AgentSensorReadingReportDto dto)
    {
        if (!currentUserContext.IsAgent)
        {
            throw new ForbiddenAccessException("Only authenticated agents may submit monitoring data.");
        }

        var agentId = currentUserContext.AgentId
            ?? throw new ForbiddenAccessException("Agent tokens require an agent identifier claim.");

        var shellyDevice = await dbContext.ShellyDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.ShellyDeviceId && x.AgentId == agentId && x.IsActive);

        if (shellyDevice is null)
        {
            throw new ForbiddenAccessException("The target Shelly device does not belong to the authenticated agent.");
        }

        var sensorReading = new SensorReading
        {
            Id = Guid.NewGuid(),
            ShellyDeviceId = dto.ShellyDeviceId,
            TemperatureCelsius = dto.TemperatureCelsius,
            BatteryPercent = dto.BatteryPercent,
            Brightness = dto.Brightness,
            DoorOpen = dto.DoorOpen,
            RecordedAtUtc = dto.RecordedAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.SensorReadings.Add(sensorReading);
        await dbContext.SaveChangesAsync();
        return sensorReading;
    }

    public async Task<MonitoredDevicePingResult> CreatePingResultAsync(AgentPingResultReportDto dto)
    {
        if (!currentUserContext.IsAgent)
        {
            throw new ForbiddenAccessException("Only authenticated agents may submit monitoring data.");
        }

        var agentId = currentUserContext.AgentId
            ?? throw new ForbiddenAccessException("Agent tokens require an agent identifier claim.");

        var monitoredDevice = await dbContext.MonitoredDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.MonitoredDeviceId && x.AgentId == agentId && x.IsActive);

        if (monitoredDevice is null)
        {
            throw new ForbiddenAccessException("The target monitored device does not belong to the authenticated agent.");
        }

        var pingResult = new MonitoredDevicePingResult
        {
            Id = Guid.NewGuid(),
            MonitoredDeviceId = dto.MonitoredDeviceId,
            IsReachable = dto.IsReachable,
            RoundtripTimeMilliseconds = dto.RoundtripTimeMilliseconds,
            ConsecutiveFailureCount = dto.ConsecutiveFailureCount,
            FailureThresholdReached = dto.FailureThresholdReached,
            ErrorMessage = dto.ErrorMessage,
            RecordedAtUtc = dto.RecordedAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Set<MonitoredDevicePingResult>().Add(pingResult);
        await dbContext.SaveChangesAsync();
        return pingResult;
    }
}
