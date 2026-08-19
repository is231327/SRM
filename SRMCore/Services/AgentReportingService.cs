using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.AgentReporting;
using SRMShared.Entities;

namespace SRMCore.Services;

public class AgentReportingService(
    SrmCoreDbContext dbContext) : IAgentReportingService
{
    public async Task<SensorReading> CreateSensorReadingAsync(AgentSensorReadingReportDto dto)
    {
        var agentId = dbContext.Agents.FirstOrDefault().Id;

        var shellyDevice = await dbContext.ShellyDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.ShellyDeviceId && x.AgentId == agentId && x.IsActive);

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
