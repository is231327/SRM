using SRMCore.Mappings.Interfaces;
using SRMShared.DTOs.SensorReading;
using SRMShared.Entities;

namespace SRMCore.Mappings;

public class SensorReadingDtoMapper : ICrudDtoMapper<SensorReading, SensorReadingCreateDto, SensorReadingUpdateDto, SensorReadingReadDto>
{
    public SensorReadingReadDto ToReadDto(SensorReading entity) => new()
    {
        Id = entity.Id,
        ShellyDeviceId = entity.ShellyDeviceId,
        TemperatureCelsius = entity.TemperatureCelsius,
        BatteryPercent = entity.BatteryPercent,
        Brightness = entity.Brightness,
        DoorOpen = entity.DoorOpen,
        RecordedAtUtc = entity.RecordedAtUtc,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc
    };

    public SensorReading ToEntity(SensorReadingCreateDto dto) => new()
    {
        ShellyDeviceId = dto.ShellyDeviceId,
        TemperatureCelsius = dto.TemperatureCelsius,
        BatteryPercent = dto.BatteryPercent,
        Brightness = dto.Brightness,
        DoorOpen = dto.DoorOpen,
        RecordedAtUtc = dto.RecordedAtUtc
    };

    public SensorReading ToEntity(SensorReadingUpdateDto dto) => new()
    {
        ShellyDeviceId = dto.ShellyDeviceId,
        TemperatureCelsius = dto.TemperatureCelsius,
        BatteryPercent = dto.BatteryPercent,
        Brightness = dto.Brightness,
        DoorOpen = dto.DoorOpen,
        RecordedAtUtc = dto.RecordedAtUtc
    };
}
