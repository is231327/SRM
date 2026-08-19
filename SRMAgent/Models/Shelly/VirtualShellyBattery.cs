using System.Text.Json.Serialization;

namespace SRMAgent.Models.Shelly;

public class VirtualShellyBattery
{
    [JsonPropertyName("value")]
    public float? Value { get; set; }
}
