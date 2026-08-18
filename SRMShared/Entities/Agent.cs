namespace SRMShared.Entities;

public class Agent : BaseEntity
{
    public Guid ServerRoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKeyReference { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string LastKnownIpAddress { get; set; } = string.Empty;
    public DateTime? LastSeenAtUtc { get; set; }
    public bool IsActive { get; set; }

    public ServerRoom? ServerRoom { get; set; }
    public ICollection<ShellyDevice> ShellyDevices { get; set; } = new List<ShellyDevice>();
    public ICollection<MonitoredDevice> MonitoredDevices { get; set; } = new List<MonitoredDevice>();
}
