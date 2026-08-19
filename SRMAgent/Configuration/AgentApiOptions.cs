namespace SRMAgent.Configuration;

public class AgentApiOptions
{
    public const string SectionName = "AgentApi";

    public string AuthBaseUrl { get; set; } = string.Empty;
    public string CoreBaseUrl { get; set; } = string.Empty;
    public string ClientIdentifier { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
