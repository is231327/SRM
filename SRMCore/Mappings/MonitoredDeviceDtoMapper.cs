using SRMCore.Mappings.Interfaces;
using SRMShared.DTOs.MonitoredDevice;
using SRMShared.Entities;

namespace SRMCore.Mappings;

public class MonitoredDeviceDtoMapper : ICrudDtoMapper<MonitoredDevice, MonitoredDeviceCreateDto, MonitoredDeviceUpdateDto, MonitoredDeviceReadDto>
{
    public MonitoredDeviceReadDto ToReadDto(MonitoredDevice entity) => new()
    {
        Id = entity.Id,
        AgentId = entity.AgentId,
        DisplayName = entity.DisplayName,
        IpAddress = entity.IpAddress,
        IntervalSeconds = entity.IntervalSeconds,
        TimeoutMilliseconds = entity.TimeoutMilliseconds,
        FailureThreshold = entity.FailureThreshold,
        IsActive = entity.IsActive,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    public MonitoredDevice ToEntity(MonitoredDeviceCreateDto dto) => new()
    {
        AgentId = dto.AgentId,
        DisplayName = dto.DisplayName,
        IpAddress = dto.IpAddress,
        IntervalSeconds = dto.IntervalSeconds,
        TimeoutMilliseconds = dto.TimeoutMilliseconds,
        FailureThreshold = dto.FailureThreshold,
        IsActive = dto.IsActive
    };

    public MonitoredDevice ToEntity(MonitoredDeviceUpdateDto dto) => new()
    {
        AgentId = dto.AgentId,
        DisplayName = dto.DisplayName,
        IpAddress = dto.IpAddress,
        IntervalSeconds = dto.IntervalSeconds,
        TimeoutMilliseconds = dto.TimeoutMilliseconds,
        FailureThreshold = dto.FailureThreshold,
        IsActive = dto.IsActive
    };
}
