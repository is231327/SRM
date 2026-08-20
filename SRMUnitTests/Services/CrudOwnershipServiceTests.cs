using SRMCore.Security;
using SRMCore.Services;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Services;

[TestFixture]
public class CrudOwnershipServiceTests
{
    [Test]
    public async Task CustomerScopedUser_ShouldOnlySeeOwnCustomer()
    {
        using var context = DbContextFactory.CreateContext();
        var ownCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();

        context.Customers.AddRange(
            new Customer { Id = ownCustomerId, Name = "Own", ExternalReference = "OWN", ContactEmail = "own@example.com", ContactPhone = "1", IsActive = true },
            new Customer { Id = otherCustomerId, Name = "Other", ExternalReference = "OTHER", ContactEmail = "other@example.com", ContactPhone = "2", IsActive = true });
        await context.SaveChangesAsync();

        var service = new CustomerService(context, new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsCustomer = true,
            IsCustomerScopedUser = true,
            CustomerId = ownCustomerId
        });

        var customers = (await service.GetAllAsync()).ToList();

        Assert.That(customers, Has.Count.EqualTo(1));
        Assert.That(customers[0].Id, Is.EqualTo(ownCustomerId));
    }

    [Test]
    public async Task CustomerScopedUser_ShouldNotReadForeignServerRoom()
    {
        using var context = DbContextFactory.CreateContext();
        var ownCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var foreignRoomId = Guid.NewGuid();

        context.ServerRooms.AddRange(
            new ServerRoom { Id = Guid.NewGuid(), CustomerId = ownCustomerId, Name = "Own Room", LocationDescription = "A", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true },
            new ServerRoom { Id = foreignRoomId, CustomerId = otherCustomerId, Name = "Foreign Room", LocationDescription = "B", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true });
        await context.SaveChangesAsync();

        var service = new ServerRoomService(context, new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsCustomerAdmin = true,
            IsCustomerScopedUser = true,
            CustomerId = ownCustomerId
        });

        var result = await service.GetByIdAsync(foreignRoomId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void CustomerScopedUser_ShouldNotCreateAgentForForeignCustomer()
    {
        using var context = DbContextFactory.CreateContext();
        var ownCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var foreignRoomId = Guid.NewGuid();

        context.ServerRooms.AddRange(
            new ServerRoom { Id = Guid.NewGuid(), CustomerId = ownCustomerId, Name = "Own Room", LocationDescription = "A", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true },
            new ServerRoom { Id = foreignRoomId, CustomerId = otherCustomerId, Name = "Foreign Room", LocationDescription = "B", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true });
        context.SaveChanges();

        var service = new AgentService(context, new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsCustomer = true,
            IsCustomerScopedUser = true,
            CustomerId = ownCustomerId
        });

        Assert.ThrowsAsync<ForbiddenAccessException>(() => service.CreateAsync(new Agent
        {
            ServerRoomId = foreignRoomId,
            Name = "Blocked Agent",
            ApiKeyReference = "key",
            Version = "1.0",
            LastKnownIpAddress = "127.0.0.1",
            LastSeenAtUtc = DateTime.UtcNow,
            IsActive = true
        }));
    }

    [Test]
    public void CustomerScopedUser_ShouldNotMoveAgentToForeignCustomerOnUpdate()
    {
        using var context = DbContextFactory.CreateContext();
        var ownCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var ownRoomId = Guid.NewGuid();
        var foreignRoomId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        context.ServerRooms.AddRange(
            new ServerRoom { Id = ownRoomId, CustomerId = ownCustomerId, Name = "Own Room", LocationDescription = "A", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true },
            new ServerRoom { Id = foreignRoomId, CustomerId = otherCustomerId, Name = "Foreign Room", LocationDescription = "B", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true });
        context.Agents.Add(new Agent
        {
            Id = agentId,
            ServerRoomId = ownRoomId,
            Name = "Own Agent",
            ApiKeyReference = "key",
            Version = "1.0",
            LastKnownIpAddress = "127.0.0.1",
            LastSeenAtUtc = DateTime.UtcNow,
            IsActive = true
        });
        context.SaveChanges();

        var service = new AgentService(context, new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsCustomerAdmin = true,
            IsCustomerScopedUser = true,
            CustomerId = ownCustomerId
        });

        Assert.ThrowsAsync<ForbiddenAccessException>(() => service.UpdateAsync(agentId, new Agent
        {
            ServerRoomId = foreignRoomId,
            Name = "Moved Agent",
            ApiKeyReference = "key-2",
            Version = "2.0",
            LastKnownIpAddress = "127.0.0.2",
            LastSeenAtUtc = DateTime.UtcNow,
            IsActive = true
        }));
    }

    [Test]
    public async Task CustomerScopedUser_ShouldNotDeleteForeignMonitoredDevice()
    {
        using var context = DbContextFactory.CreateContext();
        var ownCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var ownRoomId = Guid.NewGuid();
        var foreignRoomId = Guid.NewGuid();
        var ownAgentId = Guid.NewGuid();
        var foreignAgentId = Guid.NewGuid();
        var foreignDeviceId = Guid.NewGuid();

        context.ServerRooms.AddRange(
            new ServerRoom { Id = ownRoomId, CustomerId = ownCustomerId, Name = "Own Room", LocationDescription = "A", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true },
            new ServerRoom { Id = foreignRoomId, CustomerId = otherCustomerId, Name = "Foreign Room", LocationDescription = "B", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true });
        context.Agents.AddRange(
            new Agent { Id = ownAgentId, ServerRoomId = ownRoomId, Name = "Own Agent", ApiKeyReference = "a", Version = "1", LastKnownIpAddress = "127.0.0.1", LastSeenAtUtc = DateTime.UtcNow, IsActive = true },
            new Agent { Id = foreignAgentId, ServerRoomId = foreignRoomId, Name = "Foreign Agent", ApiKeyReference = "b", Version = "1", LastKnownIpAddress = "127.0.0.2", LastSeenAtUtc = DateTime.UtcNow, IsActive = true });
        context.MonitoredDevices.Add(new MonitoredDevice
        {
            Id = foreignDeviceId,
            AgentId = foreignAgentId,
            DisplayName = "Foreign Device",
            IpAddress = "10.0.0.1",
            IntervalSeconds = 30,
            TimeoutMilliseconds = 1000,
            FailureThreshold = 3,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = new MonitoredDeviceService(context, new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsCustomer = true,
            IsCustomerScopedUser = true,
            CustomerId = ownCustomerId
        });

        var deleted = await service.DeleteAsync(foreignDeviceId);

        Assert.That(deleted, Is.False);
    }
}
