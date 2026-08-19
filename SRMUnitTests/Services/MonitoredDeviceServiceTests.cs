using NUnit.Framework;
using SRMCore.Data;
using SRMCore.Services;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMUnitTests.Services;

[TestFixture]
public class MonitoredDeviceServiceTests : CrudServiceTestBase<MonitoredDevice>
{
    protected override ICrudService<MonitoredDevice> CreateService(SrmCoreDbContext context) => new MonitoredDeviceService(context);

    protected override MonitoredDevice CreateEntity() => new()
    {
        AgentId = Guid.NewGuid(),
        DisplayName = "Switch A",
        IpAddress = "10.0.0.1",
        IntervalSeconds = 30,
        TimeoutMilliseconds = 1000,
        FailureThreshold = 3,
        IsActive = true
    };

    protected override MonitoredDevice CreateUpdatedEntity() => new()
    {
        AgentId = Guid.NewGuid(),
        DisplayName = "Switch B",
        IpAddress = "10.0.0.2",
        IntervalSeconds = 60,
        TimeoutMilliseconds = 2000,
        FailureThreshold = 5,
        IsActive = false
    };

    protected override void AssertEntityUpdated(MonitoredDevice entity)
    {
        Assert.That(entity.DisplayName, Is.EqualTo("Switch B"));
        Assert.That(entity.IpAddress, Is.EqualTo("10.0.0.2"));
        Assert.That(entity.IsActive, Is.False);
    }
}
