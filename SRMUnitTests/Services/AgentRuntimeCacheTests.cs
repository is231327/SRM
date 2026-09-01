using SRMAgent.Services;
using SRMShared.DTOs.AgentRuntime;
using SRMShared.DTOs.MonitoredDevice;

namespace SRMUnitTests.Services;

[TestFixture]
public class AgentRuntimeCacheTests
{
    [Test]
    public void TryBeginPing_ShouldHonorConfiguredIntervalPerDevice()
    {
        var cache = new AgentRuntimeCache();
        var deviceId = Guid.NewGuid();
        var initialTime = new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);

        Assert.That(cache.TryBeginPing(deviceId, 30, initialTime), Is.True);
        Assert.That(cache.TryBeginPing(deviceId, 30, initialTime.AddSeconds(29)), Is.False);
        Assert.That(cache.TryBeginPing(deviceId, 30, initialTime.AddSeconds(30)), Is.True);
    }

    [Test]
    public void TryBeginPing_ShouldTrackDevicesIndependently()
    {
        var cache = new AgentRuntimeCache();
        var now = DateTime.UtcNow;

        Assert.That(cache.TryBeginPing(Guid.NewGuid(), 60, now), Is.True);
        Assert.That(cache.TryBeginPing(Guid.NewGuid(), 60, now), Is.True);
    }

    [Test]
    public void Update_ShouldResetPingScheduleAndFailureCountWhenIpAddressChanges()
    {
        var cache = new AgentRuntimeCache();
        var deviceId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        cache.Update(ConfigurationWithDevice(deviceId, "192.0.2.10"));
        Assert.That(cache.TryBeginPing(deviceId, 60, now), Is.True);
        Assert.That(cache.RegisterPingOutcome(deviceId, isReachable: false), Is.EqualTo(1));

        cache.Update(ConfigurationWithDevice(deviceId, "127.0.0.1"));

        Assert.That(cache.TryBeginPing(deviceId, 60, now.AddSeconds(1)), Is.True);
        Assert.That(cache.RegisterPingOutcome(deviceId, isReachable: true), Is.Zero);
    }

    private static AgentRuntimeConfigurationDto ConfigurationWithDevice(Guid id, string ipAddress)
        => new()
        {
            MonitoredDevices =
            [
                new MonitoredDeviceReadDto
                {
                    Id = id,
                    DisplayName = "Device",
                    IpAddress = ipAddress,
                    IntervalSeconds = 60,
                    TimeoutMilliseconds = 1000,
                    FailureThreshold = 3,
                    IsActive = true
                }
            ]
        };
}
