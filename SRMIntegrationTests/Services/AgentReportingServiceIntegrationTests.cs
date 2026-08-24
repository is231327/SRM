using SRMCore.Services;
using SRMIntegrationTests.TestHelpers;
using SRMShared.DTOs.AgentReporting;
using SRMShared.Entities;

namespace SRMIntegrationTests.Services;

[TestFixture]
public class AgentReportingServiceIntegrationTests
{
    [SetUp]
    public void ResetDatabase()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Test]
    public async Task CreatePingResultAsync_ShouldPersistPingResultInSqlServer()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        var customer = new Customer { Name = "Customer", ExternalReference = "INT-CUST", ContactEmail = "customer@example.com", ContactPhone = "123", IsActive = true };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var room = new ServerRoom { CustomerId = customer.Id, Name = "Room", LocationDescription = "A", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true };
        context.ServerRooms.Add(room);
        await context.SaveChangesAsync();

        var agent = new Agent { ServerRoomId = room.Id, Name = "Agent", ApiKeyReference = "ref", Version = "1.0", LastKnownIpAddress = "127.0.0.1", IsActive = true };
        context.Agents.Add(agent);
        await context.SaveChangesAsync();

        var monitoredDevice = new MonitoredDevice { AgentId = agent.Id, DisplayName = "Switch", IpAddress = "192.168.1.10", IntervalSeconds = 30, TimeoutMilliseconds = 1000, FailureThreshold = 3, IsActive = true };
        context.MonitoredDevices.Add(monitoredDevice);
        await context.SaveChangesAsync();

        var currentUserContext = new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsAgent = true,
            AgentId = agent.Id
        };

        var service = new AgentReportingService(context, currentUserContext, new FakeIncidentService());
        var created = await service.CreatePingResultAsync(new AgentPingResultReportDto
        {
            MonitoredDeviceId = monitoredDevice.Id,
            IsReachable = false,
            RoundtripTimeMilliseconds = 0,
            ConsecutiveFailureCount = 3,
            FailureThresholdReached = true,
            ErrorMessage = "TimedOut",
            RecordedAtUtc = DateTime.UtcNow
        });

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));

        var persisted = await context.MonitoredDevicePingResults.FindAsync(created.Id);
        Assert.That(persisted, Is.Not.Null);
    }
}

