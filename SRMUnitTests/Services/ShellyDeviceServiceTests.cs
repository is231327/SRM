using NUnit.Framework;
using SRMCore.Data;
using SRMCore.Services;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Services;

[TestFixture]
public class ShellyDeviceServiceTests : CrudServiceTestBase<ShellyDevice>
{
    protected override ICrudService<ShellyDevice> CreateService(SrmCoreDbContext context) => new ShellyDeviceService(context, CoreCurrentUserContextFactory.Create());

    protected override ShellyDevice CreateEntity() => new()
    {
        AgentId = Guid.NewGuid(),
        Name = "Shelly A",
        DeviceType = "DoorWindow2",
        BaseUrl = "http://shelly-a",
        MacAddress = "AA:BB:CC:DD:EE:01",
        FirmwareVersion = "1.0",
        IsVirtual = true,
        IsActive = true
    };

    protected override ShellyDevice CreateUpdatedEntity() => new()
    {
        AgentId = Guid.NewGuid(),
        Name = "Shelly B",
        DeviceType = "DoorWindow2",
        BaseUrl = "http://shelly-b",
        MacAddress = "AA:BB:CC:DD:EE:02",
        FirmwareVersion = "2.0",
        IsVirtual = false,
        IsActive = false
    };

    protected override void AssertEntityUpdated(ShellyDevice entity)
    {
        Assert.That(entity.Name, Is.EqualTo("Shelly B"));
        Assert.That(entity.BaseUrl, Is.EqualTo("http://shelly-b"));
        Assert.That(entity.IsActive, Is.False);
    }
}
