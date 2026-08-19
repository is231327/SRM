using NUnit.Framework;
using SRMCore.Data;
using SRMCore.Services;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMUnitTests.Services;

[TestFixture]
public class MaintenanceWindowServiceTests : CrudServiceTestBase<MaintenanceWindow>
{
    protected override ICrudService<MaintenanceWindow> CreateService(SrmCoreDbContext context) => new MaintenanceWindowService(context);

    protected override MaintenanceWindow CreateEntity() => new()
    {
        ServerRoomId = Guid.NewGuid(),
        Title = "Window A",
        StartUtc = DateTime.UtcNow,
        EndUtc = DateTime.UtcNow.AddHours(1),
        Description = "Planned work"
    };

    protected override MaintenanceWindow CreateUpdatedEntity() => new()
    {
        ServerRoomId = Guid.NewGuid(),
        Title = "Window B",
        StartUtc = DateTime.UtcNow.AddDays(1),
        EndUtc = DateTime.UtcNow.AddDays(1).AddHours(2),
        Description = "Updated work"
    };

    protected override void AssertEntityUpdated(MaintenanceWindow entity)
    {
        Assert.That(entity.Title, Is.EqualTo("Window B"));
        Assert.That(entity.Description, Is.EqualTo("Updated work"));
    }
}
