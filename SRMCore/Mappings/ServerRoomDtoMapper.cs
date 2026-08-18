using SRMCore.Mappings.Interfaces;
using SRMShared.DTOs.ServerRoom;
using SRMShared.Entities;

namespace SRMCore.Mappings;

public class ServerRoomDtoMapper : ICrudDtoMapper<ServerRoom, ServerRoomCreateDto, ServerRoomUpdateDto, ServerRoomReadDto>
{
    public ServerRoomReadDto ToReadDto(ServerRoom entity) => new()
    {
        Id = entity.Id,
        CustomerId = entity.CustomerId,
        Name = entity.Name,
        LocationDescription = entity.LocationDescription,
        TemperatureWarningThreshold = entity.TemperatureWarningThreshold,
        TemperatureCriticalThreshold = entity.TemperatureCriticalThreshold,
        MonitoringEnabled = entity.MonitoringEnabled,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    public ServerRoom ToEntity(ServerRoomCreateDto dto) => new()
    {
        CustomerId = dto.CustomerId,
        Name = dto.Name,
        LocationDescription = dto.LocationDescription,
        TemperatureWarningThreshold = dto.TemperatureWarningThreshold,
        TemperatureCriticalThreshold = dto.TemperatureCriticalThreshold,
        MonitoringEnabled = dto.MonitoringEnabled
    };

    public ServerRoom ToEntity(ServerRoomUpdateDto dto) => new()
    {
        CustomerId = dto.CustomerId,
        Name = dto.Name,
        LocationDescription = dto.LocationDescription,
        TemperatureWarningThreshold = dto.TemperatureWarningThreshold,
        TemperatureCriticalThreshold = dto.TemperatureCriticalThreshold,
        MonitoringEnabled = dto.MonitoringEnabled
    };
}
