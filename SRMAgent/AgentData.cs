namespace SRMAgent;

public class AgentData
{
    public string CustomerId { get; set; } = "";
    public string AgentId { get; set; } = "";
    public string ShellyId { get; set; } = "";
    public float CurrentTemp { get; set; } = 0;
    public float CurrentBattery { get; set; } = 0;
    public bool DoorOpen { get; set; } = false;
    public DateTime KeepAliveTimestamp { get; set; } = DateTime.MinValue;
}
