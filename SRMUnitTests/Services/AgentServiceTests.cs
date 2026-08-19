using NUnit.Framework;
using SRMCore.Data;
using SRMCore.Services;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMUnitTests.Services;

[TestFixture]
public class AgentServiceTests : CrudServiceTestBase<Agent>
{
    protected override ICrudService<Agent> CreateService(SrmCoreDbContext context) => new AgentService(context);

    protected override Agent CreateEntity() => new()
    {
        ServerRoomId = Guid.NewGuid(),
        Name = "Agent A",
        ApiKeyReference = "key-a",
        Version = "1.0.0",
        LastKnownIpAddress = "192.168.0.10",
        LastSeenAtUtc = DateTime.UtcNow,
        IsActive = true
    };

    protected override Agent CreateUpdatedEntity() => new()
    {
        ServerRoomId = Guid.NewGuid(),
        Name = "Agent B",
        ApiKeyReference = "key-b",
        Version = "2.0.0",
        LastKnownIpAddress = "192.168.0.11",
        LastSeenAtUtc = DateTime.UtcNow.AddMinutes(-5),
        IsActive = false
    };

    protected override void AssertEntityUpdated(Agent entity)
    {
        Assert.That(entity.Name, Is.EqualTo("Agent B"));
        Assert.That(entity.Version, Is.EqualTo("2.0.0"));
        Assert.That(entity.IsActive, Is.False);
    }
}
