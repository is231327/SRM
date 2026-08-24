using Microsoft.AspNetCore.Mvc;
using SRMCore.Controllers;
using SRMCore.Mappings;
using SRMCore.Services;
using SRMShared.DTOs.AgentReporting;
using SRMShared.DTOs.MonitoredDevicePingResult;
using SRMShared.DTOs.SensorReading;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Controllers;

[TestFixture]
public class AgentReportingControllerTests
{
    [Test]
    public async Task CreateSensorReading_ShouldReturnOk_WithReadDto()
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

        var controller = new AgentReportingController(
            new AgentReportingService(context, currentUserContext, new FakeIncidentService()),
            new SensorReadingDtoMapper(),
            new MonitoredDevicePingResultDtoMapper());

        var result = await controller.CreateSensorReading(new AgentSensorReadingReportDto
        {
            ShellyDeviceId = shelly.Id,
            TemperatureCelsius = 23,
            BatteryPercent = 88,
            Brightness = 50,
            DoorOpen = false,
            RecordedAtUtc = DateTime.UtcNow
        });

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        Assert.That(okResult.Value, Is.InstanceOf<SensorReadingReadDto>());
    }

    [Test]
    public async Task CreatePingResult_ShouldReturnOk_WithReadDto()
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

        var controller = new AgentReportingController(
            new AgentReportingService(context, currentUserContext, new FakeIncidentService()),
            new SensorReadingDtoMapper(),
            new MonitoredDevicePingResultDtoMapper());

        var result = await controller.CreatePingResult(new AgentPingResultReportDto
        {
            MonitoredDeviceId = monitoredDevice.Id,
            IsReachable = false,
            RoundtripTimeMilliseconds = 0,
            ConsecutiveFailureCount = 2,
            FailureThresholdReached = false,
            ErrorMessage = "TimedOut",
            RecordedAtUtc = DateTime.UtcNow
        });

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        Assert.That(okResult.Value, Is.InstanceOf<MonitoredDevicePingResultReadDto>());
    }
}

