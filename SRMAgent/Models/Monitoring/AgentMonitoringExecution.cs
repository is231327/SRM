using SRMShared.DTOs.AgentRuntime;

namespace SRMAgent.Models.Monitoring;

public class AgentMonitoringExecution
{
    public AgentRuntimeConfigurationDto Configuration { get; set; } = new();
    public AgentMonitoringCycleResult Result { get; set; } = new();
}
