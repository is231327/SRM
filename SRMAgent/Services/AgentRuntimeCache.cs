using SRMShared.DTOs.AgentRuntime;

namespace SRMAgent.Services;

public class AgentRuntimeCache
{
    private readonly object _syncRoot = new();
    private AgentRuntimeConfigurationDto? _currentConfiguration;
    private DateTime? _lastConfigurationRefreshAtUtc;
    private DateTime? _lastMonitoringCycleAtUtc;
    private readonly Dictionary<Guid, int> _consecutivePingFailures = [];
    private readonly Dictionary<Guid, DateTime> _lastPingAtUtc = [];

    public AgentRuntimeConfigurationDto? CurrentConfiguration
    {
        get { lock (_syncRoot) return _currentConfiguration; }
    }

    public DateTime? LastConfigurationRefreshAtUtc
    {
        get { lock (_syncRoot) return _lastConfigurationRefreshAtUtc; }
    }

    public DateTime? LastMonitoringCycleAtUtc
    {
        get { lock (_syncRoot) return _lastMonitoringCycleAtUtc; }
    }

    public void Update(AgentRuntimeConfigurationDto configuration)
    {
        lock (_syncRoot)
        {
            var previousDevices = _currentConfiguration?.MonitoredDevices
                .ToDictionary(device => device.Id) ?? [];
            _currentConfiguration = configuration;
            _lastConfigurationRefreshAtUtc = DateTime.UtcNow;

            var configuredDeviceIds = configuration.MonitoredDevices.Select(device => device.Id).ToHashSet();
            foreach (var removedDeviceId in _consecutivePingFailures.Keys.Where(id => !configuredDeviceIds.Contains(id)).ToArray())
            {
                _consecutivePingFailures.Remove(removedDeviceId);
                _lastPingAtUtc.Remove(removedDeviceId);
            }

            foreach (var device in configuration.MonitoredDevices)
            {
                if (previousDevices.TryGetValue(device.Id, out var previousDevice)
                    && HasPingConfigurationChanged(previousDevice, device))
                {
                    _consecutivePingFailures.Remove(device.Id);
                    _lastPingAtUtc.Remove(device.Id);
                }
            }
        }
    }

    private static bool HasPingConfigurationChanged(
        SRMShared.DTOs.MonitoredDevice.MonitoredDeviceReadDto previousDevice,
        SRMShared.DTOs.MonitoredDevice.MonitoredDeviceReadDto currentDevice)
        => !string.Equals(previousDevice.IpAddress, currentDevice.IpAddress, StringComparison.OrdinalIgnoreCase)
            || previousDevice.IntervalSeconds != currentDevice.IntervalSeconds
            || previousDevice.TimeoutMilliseconds != currentDevice.TimeoutMilliseconds
            || previousDevice.FailureThreshold != currentDevice.FailureThreshold
            || previousDevice.IsActive != currentDevice.IsActive;

    public void MarkCycleExecuted()
    {
        lock (_syncRoot)
        {
            _lastMonitoringCycleAtUtc = DateTime.UtcNow;
        }
    }

    public bool ShouldRefreshConfiguration(int refreshIntervalSeconds)
    {
        lock (_syncRoot)
        {
            if (_currentConfiguration is null || !_lastConfigurationRefreshAtUtc.HasValue)
            {
                return true;
            }

            return DateTime.UtcNow - _lastConfigurationRefreshAtUtc.Value >= TimeSpan.FromSeconds(refreshIntervalSeconds);
        }
    }

    public bool TryBeginPing(Guid monitoredDeviceId, int intervalSeconds, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        lock (_syncRoot)
        {
            if (_lastPingAtUtc.TryGetValue(monitoredDeviceId, out var lastPingAtUtc)
                && now - lastPingAtUtc < TimeSpan.FromSeconds(Math.Max(1, intervalSeconds)))
            {
                return false;
            }

            _lastPingAtUtc[monitoredDeviceId] = now;
            return true;
        }
    }

    public int RegisterPingOutcome(Guid monitoredDeviceId, bool isReachable)
    {
        lock (_syncRoot)
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
}
