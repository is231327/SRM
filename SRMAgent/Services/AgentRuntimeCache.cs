using SRMShared.DTOs.AgentRuntime;

namespace SRMAgent.Services;

public class AgentRuntimeCache
{
    public AgentRuntimeConfigurationDto? CurrentConfiguration { get; private set; }
    public DateTime? LastConfigurationRefreshAtUtc { get; private set; }
    public DateTime? LastMonitoringCycleAtUtc { get; private set; }
    private readonly Dictionary<Guid, int> _consecutivePingFailures = [];

    public void Update(AgentRuntimeConfigurationDto configuration)
    {
        CurrentConfiguration = configuration;
        LastConfigurationRefreshAtUtc = DateTime.UtcNow;
    }

    public void MarkCycleExecuted()
    {
        LastMonitoringCycleAtUtc = DateTime.UtcNow;
    }

    public bool ShouldRefreshConfiguration(int refreshIntervalSeconds)
    {
        if (CurrentConfiguration is null || !LastConfigurationRefreshAtUtc.HasValue)
        {
            return true;
        }

        return DateTime.UtcNow - LastConfigurationRefreshAtUtc.Value >= TimeSpan.FromSeconds(refreshIntervalSeconds);
    }

    public int RegisterPingOutcome(Guid monitoredDeviceId, bool isReachable)
    {
        if (isReachable)
        {
            _consecutivePingFailures[monitoredDeviceId] = 0;
            return 0;
        }

        var next = _consecutivePingFailures.TryGetValue(monitoredDeviceId, out var current) ? current + 1 : 1;
        _consecutivePingFailures[monitoredDeviceId] = next;
        return next;
    }
}
