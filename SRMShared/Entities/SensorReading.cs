namespace SRMShared.Entities;

public class SensorReading : BaseEntity
{
    public Guid ShellyDeviceId { get; set; }
    public float TemperatureCelsius { get; set; }
    public float BatteryPercent { get; set; }
    public float Brightness { get; set; }
    public bool DoorOpen { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

    public ShellyDevice? ShellyDevice { get; set; }
}
