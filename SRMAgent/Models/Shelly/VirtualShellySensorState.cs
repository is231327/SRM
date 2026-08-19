using System.Text.Json.Serialization;

namespace SRMAgent.Models.Shelly;

public class VirtualShellySensorState
{
    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
}
