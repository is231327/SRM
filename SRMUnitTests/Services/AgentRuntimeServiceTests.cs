using SRMCore.Mappings;
using SRMCore.Services;
using SRMShared.DTOs.AgentRuntime;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Services;

[TestFixture]
public class AgentRuntimeServiceTests
{
    [Test]
    public async Task GetRuntimeConfigurationAsync_ShouldReturnAssignedAgentConfiguration()
    {
        using var context = DbContextFactory.CreateContext();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Customer", ContactEmail = "customer@example.com", ContactPhone = "123" };
        var serverRoom = new ServerRoom { Id = Guid.NewGuid(), CustomerId = customer.Id, Name = "Room", LocationDescription = "A", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true };
        var agent = new Agent { Id = Guid.NewGuid(), ServerRoomId = serverRoom.Id, Name = "Agent", ApiKeyReference = "ref", Version = "1.0", LastKnownIpAddress = "127.0.0.1", IsActive = true };
        var shelly = new ShellyDevice { Id = Guid.NewGuid(), AgentId = agent.Id, Name = "Shelly", DeviceType = "Door", BaseUrl = "http://localhost:8080", MacAddress = "AA:BB:CC:DD:EE:FF", FirmwareVersion = "1.0", IsActive = true };
        var monitoredDevice = new MonitoredDevice { Id = Guid.NewGuid(), AgentId = agent.Id, DisplayName = "Switch", IpAddress = "192.168.1.10", IntervalSeconds = 30, TimeoutMilliseconds = 1000, FailureThreshold = 3, IsActive = true };

        context.Customers.Add(customer);
        context.ServerRooms.Add(serverRoom);
        context.Agents.Add(agent);
        context.ShellyDevices.Add(shelly);
        context.MonitoredDevices.Add(monitoredDevice);
        await context.SaveChangesAsync();

        var currentUserContext = new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsAgent = true,
            AgentId = agent.Id
        };

        var service = new AgentRuntimeService(
            context,
            currentUserContext,
            new AgentDtoMapper(),
            new ShellyDeviceDtoMapper(),
            new MonitoredDeviceDtoMapper());

        var configuration = await service.GetRuntimeConfigurationAsync();

        Assert.That(configuration, Is.Not.Null);
        Assert.That(configuration!.Agent.Id, Is.EqualTo(agent.Id));
        Assert.That(configuration.ShellyDevices.Count, Is.EqualTo(1));
        Assert.That(configuration.MonitoredDevices.Count, Is.EqualTo(1));
    }
}
