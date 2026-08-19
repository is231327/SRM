using System.Text.Json.Serialization;

namespace SRMAgent.Models.Shelly;

public class VirtualShellyLux
{
    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("value")]
    public float? Value { get; set; }
}
