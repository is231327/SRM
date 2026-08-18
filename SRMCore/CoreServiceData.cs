public class CoreServiceData
{
    public string CustomerId { get; set; } = "";
    public string AgentId { get; set; } = "";
    public string ShellyId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public float CurrentTemp { get; set; } = 0;
    public float CurrentBattery { get; set; } = 0;
    public bool DoorOpen { get; set; } = false;
    public DateTime KeepAliveTimestamp { get; set; } = DateTime.MinValue;
}
