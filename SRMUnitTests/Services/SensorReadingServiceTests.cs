using NUnit.Framework;
using SRMCore.Data;
using SRMCore.Services;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Services;

[TestFixture]
public class SensorReadingServiceTests : CrudServiceTestBase<SensorReading>
{
    protected override ICrudService<SensorReading> CreateService(SrmCoreDbContext context) => new SensorReadingService(context, CoreCurrentUserContextFactory.Create());

    protected override SensorReading CreateEntity() => new()
    {
        ShellyDeviceId = Guid.NewGuid(),
        TemperatureCelsius = 22.5f,
        BatteryPercent = 80,
        Brightness = 100,
        DoorOpen = false,
        RecordedAtUtc = DateTime.UtcNow
    };

    protected override SensorReading CreateUpdatedEntity() => new()
    {
        ShellyDeviceId = Guid.NewGuid(),
        TemperatureCelsius = 24.5f,
        BatteryPercent = 60,
        Brightness = 200,
        DoorOpen = true,
        RecordedAtUtc = DateTime.UtcNow.AddMinutes(1)
    };

    protected override void AssertEntityUpdated(SensorReading entity)
    {
        Assert.That(entity.TemperatureCelsius, Is.EqualTo(24.5f));
        Assert.That(entity.DoorOpen, Is.True);
        Assert.That(entity.BatteryPercent, Is.EqualTo(60));
    }
}
