using SRMCore.Data;
using SRMCore.Services;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Services;

[TestFixture]
public class ServerRoomServiceTests : CrudServiceTestBase<ServerRoom>
{
    protected override ICrudService<ServerRoom> CreateService(SrmCoreDbContext context) => new ServerRoomService(context, CoreCurrentUserContextFactory.Create());

    protected override ServerRoom CreateEntity() => new()
    {
        CustomerId = Guid.NewGuid(),
        Name = "Room A",
        LocationDescription = "First floor",
        TemperatureWarningThreshold = 25,
        TemperatureCriticalThreshold = 30,
        MonitoringEnabled = true
    };

    protected override ServerRoom CreateUpdatedEntity() => new()
    {
        CustomerId = Guid.NewGuid(),
        Name = "Room B",
        LocationDescription = "Second floor",
        TemperatureWarningThreshold = 26,
        TemperatureCriticalThreshold = 31,
        MonitoringEnabled = false
    };

    protected override void AssertEntityUpdated(ServerRoom entity)
    {
        Assert.That(entity.Name, Is.EqualTo("Room B"));
        Assert.That(entity.LocationDescription, Is.EqualTo("Second floor"));
        Assert.That(entity.MonitoringEnabled, Is.False);
    }
}
