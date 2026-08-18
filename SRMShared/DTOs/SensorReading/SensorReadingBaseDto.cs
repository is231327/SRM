using System.ComponentModel.DataAnnotations;
using SRMShared.Attributes;

namespace SRMShared.DTOs.SensorReading;

public class SensorReadingBaseDto
{
    [NonEmptyGuid]
    public Guid ShellyDeviceId { get; set; }

    [Range(-50, 100)]
    public float TemperatureCelsius { get; set; }

    [Range(0, 100)]
    public float BatteryPercent { get; set; }

    [Range(0, 100000)]
    public float Brightness { get; set; }

    public bool DoorOpen { get; set; }

    public DateTime RecordedAtUtc { get; set; }
}
