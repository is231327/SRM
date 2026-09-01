using SRMCore.Data;
using SRMCore.Services;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Services;

[TestFixture]
public class MonitoredDeviceServiceTests : CrudServiceTestBase<MonitoredDevice>
{
    protected override ICrudService<MonitoredDevice> CreateService(SrmCoreDbContext context) => new MonitoredDeviceService(context, CoreCurrentUserContextFactory.Create());

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

    [Test]
    public async Task DeleteAsync_PreservesIncidentAndClearsDeviceReference()
    {
        using var context = DbContextFactory.CreateContext();
        var device = CreateEntity();
        device.Id = Guid.NewGuid();
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            ServerRoomId = Guid.NewGuid(),
            MonitoredDeviceId = device.Id,
            Summary = "Device unreachable",
            Description = "Test",
            CorrelationKey = "ping:test"
        };
        context.AddRange(device, incident);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var deleted = await service.DeleteAsync(device.Id);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.True);
            Assert.That(context.MonitoredDevices.Find(device.Id), Is.Null);
            Assert.That(context.Incidents.Find(incident.Id), Is.Not.Null);
            Assert.That(context.Incidents.Find(incident.Id)!.MonitoredDeviceId, Is.Null);
        });
    }
}
