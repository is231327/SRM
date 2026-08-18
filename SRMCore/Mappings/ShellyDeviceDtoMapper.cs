using SRMCore.Mappings.Interfaces;
using SRMShared.DTOs.ShellyDevice;
using SRMShared.Entities;

namespace SRMCore.Mappings;

public class ShellyDeviceDtoMapper : ICrudDtoMapper<ShellyDevice, ShellyDeviceCreateDto, ShellyDeviceUpdateDto, ShellyDeviceReadDto>
{
    public ShellyDeviceReadDto ToReadDto(ShellyDevice entity) => new()
    {
        Id = entity.Id,
        AgentId = entity.AgentId,
        Name = entity.Name,
        DeviceType = entity.DeviceType,
        BaseUrl = entity.BaseUrl,
        MacAddress = entity.MacAddress,
        FirmwareVersion = entity.FirmwareVersion,
        IsVirtual = entity.IsVirtual,
        IsActive = entity.IsActive,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    public ShellyDevice ToEntity(ShellyDeviceCreateDto dto) => new()
    {
        AgentId = dto.AgentId,
        Name = dto.Name,
        DeviceType = dto.DeviceType,
        BaseUrl = dto.BaseUrl,
        MacAddress = dto.MacAddress,
        FirmwareVersion = dto.FirmwareVersion,
        IsVirtual = dto.IsVirtual,
        IsActive = dto.IsActive
    };

    public ShellyDevice ToEntity(ShellyDeviceUpdateDto dto) => new()
    {
        AgentId = dto.AgentId,
        Name = dto.Name,
        DeviceType = dto.DeviceType,
        BaseUrl = dto.BaseUrl,
        MacAddress = dto.MacAddress,
        FirmwareVersion = dto.FirmwareVersion,
        IsVirtual = dto.IsVirtual,
        IsActive = dto.IsActive
    };
}
