using SRMCore.Security;
using SRMCore.Services;
using SRMIntegrationTests.TestHelpers;
using SRMShared.Entities;

namespace SRMIntegrationTests.Services;

[TestFixture]
public class CrudOwnershipIntegrationTests
{
    [SetUp]
    public void ResetDatabase()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Test]
    public async Task CustomerScopedUser_ShouldOnlySeeOwnCustomer_InSqlServer()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
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
    public async Task CustomerScopedUser_ShouldNotReadForeignServerRoom_InSqlServer()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        var ownCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var foreignRoomId = Guid.NewGuid();

        context.Customers.AddRange(
            new Customer { Id = ownCustomerId, Name = "Own", ExternalReference = "OWN", ContactEmail = "own@example.com", ContactPhone = "1", IsActive = true },
            new Customer { Id = otherCustomerId, Name = "Other", ExternalReference = "OTHER", ContactEmail = "other@example.com", ContactPhone = "2", IsActive = true });
        await context.SaveChangesAsync();

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
    public void CustomerScopedUser_ShouldNotCreateAgentForForeignCustomer_InSqlServer()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        var ownCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var foreignRoomId = Guid.NewGuid();

        context.Customers.AddRange(
            new Customer { Id = ownCustomerId, Name = "Own", ExternalReference = "OWN", ContactEmail = "own@example.com", ContactPhone = "1", IsActive = true },
            new Customer { Id = otherCustomerId, Name = "Other", ExternalReference = "OTHER", ContactEmail = "other@example.com", ContactPhone = "2", IsActive = true });
        context.SaveChanges();

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
    public void CustomerScopedUser_ShouldNotMoveAgentToForeignCustomerOnUpdate_InSqlServer()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        var ownCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var ownRoomId = Guid.NewGuid();
        var foreignRoomId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        context.Customers.AddRange(
            new Customer { Id = ownCustomerId, Name = "Own", ExternalReference = "OWN", ContactEmail = "own@example.com", ContactPhone = "1", IsActive = true },
            new Customer { Id = otherCustomerId, Name = "Other", ExternalReference = "OTHER", ContactEmail = "other@example.com", ContactPhone = "2", IsActive = true });
        context.SaveChanges();

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
    public async Task CustomerScopedUser_ShouldNotDeleteForeignMonitoredDevice_InSqlServer()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        var ownCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var ownRoomId = Guid.NewGuid();
        var foreignRoomId = Guid.NewGuid();
        var ownAgentId = Guid.NewGuid();
        var foreignAgentId = Guid.NewGuid();
        var foreignDeviceId = Guid.NewGuid();

        context.Customers.AddRange(
            new Customer { Id = ownCustomerId, Name = "Own", ExternalReference = "OWN", ContactEmail = "own@example.com", ContactPhone = "1", IsActive = true },
            new Customer { Id = otherCustomerId, Name = "Other", ExternalReference = "OTHER", ContactEmail = "other@example.com", ContactPhone = "2", IsActive = true });
        await context.SaveChangesAsync();

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

    [Test]
    public async Task DeleteMonitoredDevice_WithHistoricalIncident_PreservesIncident_InSqlServer()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        var customerId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();

        context.Customers.Add(new Customer
        {
            Id = customerId,
            Name = "Delete Test",
            ExternalReference = "DELETE-TEST",
            ContactEmail = "delete@example.com",
            ContactPhone = "1",
            IsActive = true
        });
        context.ServerRooms.Add(new ServerRoom
        {
            Id = roomId,
            CustomerId = customerId,
            Name = "Room",
            LocationDescription = "Test",
            TemperatureWarningThreshold = 25,
            TemperatureCriticalThreshold = 30,
            MonitoringEnabled = true
        });
        context.Agents.Add(new Agent
        {
            Id = agentId,
            ServerRoomId = roomId,
            Name = "Agent",
            ApiKeyReference = "key",
            Version = "1",
            LastKnownIpAddress = "127.0.0.1",
            LastSeenAtUtc = DateTime.UtcNow,
            IsActive = true
        });
        context.MonitoredDevices.Add(new MonitoredDevice
        {
            Id = deviceId,
            AgentId = agentId,
            DisplayName = "Switch",
            IpAddress = "10.0.0.1",
            IntervalSeconds = 30,
            TimeoutMilliseconds = 1000,
            FailureThreshold = 3,
            IsActive = true
        });
        context.Incidents.Add(new Incident
        {
            Id = incidentId,
            ServerRoomId = roomId,
            MonitoredDeviceId = deviceId,
            Summary = "Switch unreachable",
            Description = "Historical incident",
            CorrelationKey = "ping:switch"
        });
        await context.SaveChangesAsync();

        var service = new MonitoredDeviceService(context, CoreCurrentUserContextFactory.Create());
        var deleted = await service.DeleteAsync(deviceId);

        context.ChangeTracker.Clear();
        var persistedIncident = await context.Incidents.FindAsync(incidentId);
        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.True);
            Assert.That(context.MonitoredDevices.Find(deviceId), Is.Null);
            Assert.That(persistedIncident, Is.Not.Null);
            Assert.That(persistedIncident!.MonitoredDeviceId, Is.Null);
        });
    }
}
