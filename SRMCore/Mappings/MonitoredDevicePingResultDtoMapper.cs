using SRMCore.Mappings.Interfaces;
using SRMShared.DTOs.MonitoredDevicePingResult;
using SRMShared.Entities;

namespace SRMCore.Mappings;

public class MonitoredDevicePingResultDtoMapper : ICrudDtoMapper<MonitoredDevicePingResult, MonitoredDevicePingResultCreateDto, MonitoredDevicePingResultUpdateDto, MonitoredDevicePingResultReadDto>
{
    public MonitoredDevicePingResultReadDto ToReadDto(MonitoredDevicePingResult entity) => new()
    {
        Id = entity.Id,
        MonitoredDeviceId = entity.MonitoredDeviceId,
        IsReachable = entity.IsReachable,
        RoundtripTimeMilliseconds = entity.RoundtripTimeMilliseconds,
        ConsecutiveFailureCount = entity.ConsecutiveFailureCount,
        FailureThresholdReached = entity.FailureThresholdReached,
        ErrorMessage = entity.ErrorMessage,
        RecordedAtUtc = entity.RecordedAtUtc,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    public MonitoredDevicePingResult ToEntity(MonitoredDevicePingResultCreateDto dto) => new()
    {
        MonitoredDeviceId = dto.MonitoredDeviceId,
        IsReachable = dto.IsReachable,
        RoundtripTimeMilliseconds = dto.RoundtripTimeMilliseconds,
        ConsecutiveFailureCount = dto.ConsecutiveFailureCount,
        FailureThresholdReached = dto.FailureThresholdReached,
        ErrorMessage = dto.ErrorMessage,
        RecordedAtUtc = dto.RecordedAtUtc
    };

    public MonitoredDevicePingResult ToEntity(MonitoredDevicePingResultUpdateDto dto) => new()
    {
        MonitoredDeviceId = dto.MonitoredDeviceId,
        IsReachable = dto.IsReachable,
        RoundtripTimeMilliseconds = dto.RoundtripTimeMilliseconds,
        ConsecutiveFailureCount = dto.ConsecutiveFailureCount,
        FailureThresholdReached = dto.FailureThresholdReached,
        ErrorMessage = dto.ErrorMessage,
        RecordedAtUtc = dto.RecordedAtUtc
    };
}
