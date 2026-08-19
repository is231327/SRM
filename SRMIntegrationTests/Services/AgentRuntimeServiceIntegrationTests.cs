using SRMCore.Mappings;
using SRMCore.Services;
using SRMIntegrationTests.TestHelpers;
using SRMShared.Entities;

namespace SRMIntegrationTests.Services;

[TestFixture]
public class AgentRuntimeServiceIntegrationTests
{
    [SetUp]
    public void ResetDatabase()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Test]
    public async Task GetRuntimeConfigurationAsync_ShouldLoadAssignedAgentConfigurationFromSqlServer()
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

        context.ShellyDevices.Add(new ShellyDevice { AgentId = agent.Id, Name = "Shelly", DeviceType = "Door", BaseUrl = "http://localhost:8080", MacAddress = "AA:BB:CC:DD:EE:FF", FirmwareVersion = "1.0", IsActive = true });
        context.MonitoredDevices.Add(new MonitoredDevice { AgentId = agent.Id, DisplayName = "Switch", IpAddress = "192.168.1.10", IntervalSeconds = 30, TimeoutMilliseconds = 1000, FailureThreshold = 3, IsActive = true });
        await context.SaveChangesAsync();

        var currentUserContext = new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsAgent = true,
            AgentId = agent.Id
        };

        var service = new AgentRuntimeService(context, currentUserContext, new AgentDtoMapper(), new ShellyDeviceDtoMapper(), new MonitoredDeviceDtoMapper());
        var runtime = await service.GetRuntimeConfigurationAsync();

        Assert.That(runtime, Is.Not.Null);
        Assert.That(runtime!.ShellyDevices.Count, Is.EqualTo(1));
        Assert.That(runtime.MonitoredDevices.Count, Is.EqualTo(1));
    }
}
