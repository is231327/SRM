namespace SRMShared.Entities;

public class ShellyDevice : BaseEntity
{
    public Guid AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public bool IsVirtual { get; set; }
    public bool IsActive { get; set; }

    public Agent? Agent { get; set; }
    public ICollection<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();
}
