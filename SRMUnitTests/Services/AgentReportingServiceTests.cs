using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SRMCore.Security;
using SRMCore.Services;
using SRMShared.DTOs.AgentReporting;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Services;

[TestFixture]
public class AgentReportingServiceTests
{
    [Test]
    public async Task CreateSensorReadingAsync_ShouldPersistReading_ForMatchingAuthenticatedAgent()
    {
        using var context = DbContextFactory.CreateContext();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Customer", ContactEmail = "customer@example.com", ContactPhone = "123" };
        var serverRoom = new ServerRoom { Id = Guid.NewGuid(), CustomerId = customer.Id, Name = "Room", LocationDescription = "A", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true };
        var agent = new Agent { Id = Guid.NewGuid(), ServerRoomId = serverRoom.Id, Name = "Agent", ApiKeyReference = "ref", Version = "1.0", LastKnownIpAddress = "127.0.0.1", IsActive = true };
        var shelly = new ShellyDevice { Id = Guid.NewGuid(), AgentId = agent.Id, Name = "Shelly", DeviceType = "Door", BaseUrl = "https://device.local", MacAddress = "AA:BB:CC:DD:EE:FF", FirmwareVersion = "1.0", IsActive = true };

        context.Customers.Add(customer);
        context.ServerRooms.Add(serverRoom);
        context.Agents.Add(agent);
        context.ShellyDevices.Add(shelly);
        await context.SaveChangesAsync();

        var currentUserContext = new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsAgent = true,
            AgentId = agent.Id
        };

        var service = new AgentReportingService(context, currentUserContext);
        var dto = new AgentSensorReadingReportDto
        {
            ShellyDeviceId = shelly.Id,
            TemperatureCelsius = 21.5f,
            BatteryPercent = 72,
            Brightness = 120,
            DoorOpen = true,
            RecordedAtUtc = DateTime.UtcNow
        };

        var created = await service.CreateSensorReadingAsync(dto);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.ShellyDeviceId, Is.EqualTo(shelly.Id));
        Assert.That(await context.SensorReadings.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public void CreateSensorReadingAsync_ShouldThrow_ForShellyDeviceOfDifferentAgent()
    {
        using var context = DbContextFactory.CreateContext();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Customer", ContactEmail = "customer@example.com", ContactPhone = "123" };
        var serverRoom = new ServerRoom { Id = Guid.NewGuid(), CustomerId = customer.Id, Name = "Room", LocationDescription = "A", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true };
        var owningAgent = new Agent { Id = Guid.NewGuid(), ServerRoomId = serverRoom.Id, Name = "AgentA", ApiKeyReference = "ref-a", Version = "1.0", LastKnownIpAddress = "127.0.0.1", IsActive = true };
        var otherAgent = new Agent { Id = Guid.NewGuid(), ServerRoomId = serverRoom.Id, Name = "AgentB", ApiKeyReference = "ref-b", Version = "1.0", LastKnownIpAddress = "127.0.0.2", IsActive = true };
        var shelly = new ShellyDevice { Id = Guid.NewGuid(), AgentId = owningAgent.Id, Name = "Shelly", DeviceType = "Door", BaseUrl = "https://device.local", MacAddress = "AA:BB:CC:DD:EE:FF", FirmwareVersion = "1.0", IsActive = true };

        context.Customers.Add(customer);
        context.ServerRooms.Add(serverRoom);
        context.Agents.AddRange(owningAgent, otherAgent);
        context.ShellyDevices.Add(shelly);
        context.SaveChanges();

        var currentUserContext = new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsAgent = true,
            AgentId = otherAgent.Id
        };

        var service = new AgentReportingService(context, currentUserContext);

        Assert.ThrowsAsync<ForbiddenAccessException>(() => service.CreateSensorReadingAsync(new AgentSensorReadingReportDto
        {
            ShellyDeviceId = shelly.Id,
            TemperatureCelsius = 20,
            BatteryPercent = 70,
            Brightness = 0,
            DoorOpen = false,
            RecordedAtUtc = DateTime.UtcNow
        }));
    }

    [Test]
    public async Task CreatePingResultAsync_ShouldPersistPingResult_ForMatchingAuthenticatedAgent()
    {
        using var context = DbContextFactory.CreateContext();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Customer", ContactEmail = "customer@example.com", ContactPhone = "123" };
        var serverRoom = new ServerRoom { Id = Guid.NewGuid(), CustomerId = customer.Id, Name = "Room", LocationDescription = "A", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true };
        var agent = new Agent { Id = Guid.NewGuid(), ServerRoomId = serverRoom.Id, Name = "Agent", ApiKeyReference = "ref", Version = "1.0", LastKnownIpAddress = "127.0.0.1", IsActive = true };
        var monitoredDevice = new MonitoredDevice { Id = Guid.NewGuid(), AgentId = agent.Id, DisplayName = "Switch", IpAddress = "192.168.1.10", IntervalSeconds = 30, TimeoutMilliseconds = 1000, FailureThreshold = 3, IsActive = true };

        context.Customers.Add(customer);
        context.ServerRooms.Add(serverRoom);
        context.Agents.Add(agent);
        context.MonitoredDevices.Add(monitoredDevice);
        await context.SaveChangesAsync();

        var currentUserContext = new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsAgent = true,
            AgentId = agent.Id
        };

        var service = new AgentReportingService(context, currentUserContext);

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
        Assert.That(created.MonitoredDeviceId, Is.EqualTo(monitoredDevice.Id));
        Assert.That(await context.MonitoredDevicePingResults.CountAsync(), Is.EqualTo(1));
    }
}
