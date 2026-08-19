using System.Text.Json.Serialization;

namespace SRMAgent.Models.Shelly;

public class VirtualShellyStatusResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("sensor")]
    public VirtualShellySensorState? Sensor { get; set; }

    [JsonPropertyName("tmp")]
    public VirtualShellyTemperature? Temperature { get; set; }

    [JsonPropertyName("bat")]
    public VirtualShellyBattery? Battery { get; set; }

    [JsonPropertyName("lux")]
    public VirtualShellyLux? Lux { get; set; }
}
