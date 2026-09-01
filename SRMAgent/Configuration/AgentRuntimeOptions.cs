namespace SRMAgent.Configuration;

public class AgentRuntimeOptions
{
    public const string SectionName = "AgentRuntime";

    public int PollingIntervalSeconds { get; set; } = 30;
    public int ConfigurationRefreshIntervalSeconds { get; set; } = 30;
    public string ShellyStatusPath { get; set; } = "status";
}
