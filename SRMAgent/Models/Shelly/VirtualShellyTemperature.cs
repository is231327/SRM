using System.Text.Json.Serialization;

namespace SRMAgent.Models.Shelly;

public class VirtualShellyTemperature
{
    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("tC")]
    public float? Celsius { get; set; }

    [JsonPropertyName("value")]
    public float? Value { get; set; }
}
