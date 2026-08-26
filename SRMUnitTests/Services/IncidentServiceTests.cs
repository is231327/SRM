using Microsoft.EntityFrameworkCore;
using SRMCore.Services;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Services;

[TestFixture]
public class IncidentServiceTests
{
    [Test]
    public async Task EvaluateSensorReadingAsync_ShouldCreateDoorIncidentAndQueueTicket_WhenDoorOpensOutsideMaintenanceWindow()
    {
        using var context = DbContextFactory.CreateContext();
        var data = await SeedShellyScenarioAsync(context);
        var service = new IncidentService(context, new TicketDispatchService(context));

        var reading = new SensorReading
        {
            ShellyDeviceId = data.ShellyDevice.Id,
            TemperatureCelsius = 20,
            BatteryPercent = 70,
            Brightness = 0,
            DoorOpen = true,
            RecordedAtUtc = DateTime.UtcNow
        };

        context.SensorReadings.Add(reading);
        await context.SaveChangesAsync();

        await service.EvaluateSensorReadingAsync(reading);

        var incident = await context.Incidents.SingleAsync();
        var ticketLink = await context.TicketLinks.SingleAsync();

        Assert.That(incident.Type, Is.EqualTo(IncidentType.DoorOpenOutsideMaintenanceWindow));
        Assert.That(incident.Status, Is.EqualTo(IncidentStatus.Open));
        Assert.That(ticketLink.SyncStatus, Is.EqualTo(TicketSyncStatus.PendingCreate));
    }

    [Test]
    public async Task EvaluateSensorReadingAsync_ShouldResolveDoorIncidentAndQueueComment_WhenDoorCloses()
    {
        using var context = DbContextFactory.CreateContext();
        var data = await SeedShellyScenarioAsync(context);
        var service = new IncidentService(context, new TicketDispatchService(context));
        var openedAt = DateTime.UtcNow.AddMinutes(-1);

        var openIncident = new Incident
        {
            ServerRoomId = data.ServerRoom.Id,
            ShellyDeviceId = data.ShellyDevice.Id,
            Type = IncidentType.DoorOpenOutsideMaintenanceWindow,
            Severity = IncidentSeverity.Critical,
            Status = IncidentStatus.Open,
            CorrelationKey = $"DoorOpenOutsideMaintenanceWindow:{data.ServerRoom.Id}:{data.ShellyDevice.Id}:none",
            Summary = "Door open",
            Description = "Open",
            OpenedAtUtc = openedAt,
            LastOccurredAtUtc = openedAt
        };

        context.Incidents.Add(openIncident);
        context.TicketLinks.Add(new TicketLink
        {
            IncidentId = openIncident.Id,
            ProviderName = "Redmine",
            ExternalTicketId = "RM-1",
            ExternalTicketUrl = "https://redmine.example.local/issues/1",
            SyncStatus = TicketSyncStatus.Created
        });
        await context.SaveChangesAsync();

        var reading = new SensorReading
        {
            ShellyDeviceId = data.ShellyDevice.Id,
            TemperatureCelsius = 20,
            BatteryPercent = 70,
            Brightness = 0,
            DoorOpen = false,
            RecordedAtUtc = DateTime.UtcNow
        };

        context.SensorReadings.Add(reading);
        await context.SaveChangesAsync();

        await service.EvaluateSensorReadingAsync(reading);

        var incident = await context.Incidents.SingleAsync();
        var ticketLink = await context.TicketLinks.SingleAsync();

        Assert.That(incident.Status, Is.EqualTo(IncidentStatus.Resolved));
        Assert.That(incident.ResolvedAtUtc, Is.Not.Null);
        Assert.That(ticketLink.SyncStatus, Is.EqualTo(TicketSyncStatus.PendingComment));
    }

    [Test]
    public async Task EvaluateSensorReadingAsync_ShouldCreateWarningTemperatureIncidentImmediately()
    {
        using var context = DbContextFactory.CreateContext();
        var data = await SeedShellyScenarioAsync(context);
        var service = new IncidentService(context, new TicketDispatchService(context));

        var reading = new SensorReading
        {
            ShellyDeviceId = data.ShellyDevice.Id,
            TemperatureCelsius = 26,
            BatteryPercent = 70,
            Brightness = 0,
            DoorOpen = false,
            RecordedAtUtc = DateTime.UtcNow
        };

        context.SensorReadings.Add(reading);
        await context.SaveChangesAsync();

        await service.EvaluateSensorReadingAsync(reading);

        var incident = await context.Incidents.SingleAsync();
        Assert.That(incident.Type, Is.EqualTo(IncidentType.TemperatureWarningThresholdExceeded));
        Assert.That(incident.Severity, Is.EqualTo(IncidentSeverity.Warning));
        Assert.That(await context.TicketLinks.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task EvaluatePingResultAsync_ShouldCreateFailureIncident_WhenThresholdReached()
    {
        using var context = DbContextFactory.CreateContext();
        var data = await SeedMonitoredDeviceScenarioAsync(context);
        var service = new IncidentService(context, new TicketDispatchService(context));

        var pingResult = new MonitoredDevicePingResult
        {
            MonitoredDeviceId = data.MonitoredDevice.Id,
            IsReachable = false,
            RoundtripTimeMilliseconds = 0,
            ConsecutiveFailureCount = 3,
            FailureThresholdReached = true,
            ErrorMessage = "TimedOut",
            RecordedAtUtc = DateTime.UtcNow
        };

        context.MonitoredDevicePingResults.Add(pingResult);
        await context.SaveChangesAsync();

        await service.EvaluatePingResultAsync(pingResult);

        var incident = await context.Incidents.SingleAsync();
        Assert.That(incident.Type, Is.EqualTo(IncidentType.MonitoredDeviceFailureThresholdReached));
        Assert.That(await context.TicketLinks.CountAsync(), Is.EqualTo(1));
    }

    private static async Task<(ServerRoom ServerRoom, ShellyDevice ShellyDevice)> SeedShellyScenarioAsync(SRMCore.Data.SrmCoreDbContext context)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Customer", ContactEmail = "customer@example.com", ContactPhone = "123", ExternalReference = "C1", IsActive = true };
        var serverRoom = new ServerRoom
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Name = "Room A",
            LocationDescription = "A",
            TemperatureWarningThreshold = 25,
            TemperatureCriticalThreshold = 30,
            MonitoringEnabled = true
        };
        var agent = new Agent { Id = Guid.NewGuid(), ServerRoomId = serverRoom.Id, Name = "Agent", ApiKeyReference = "ref", Version = "1.0", LastKnownIpAddress = "127.0.0.1", IsActive = true };
        var shelly = new ShellyDevice { Id = Guid.NewGuid(), AgentId = agent.Id, Name = "Shelly", DeviceType = "Door", BaseUrl = "http://device.local", MacAddress = "AA:BB:CC:DD:EE:FF", FirmwareVersion = "1.0", IsActive = true };

        context.Customers.Add(customer);
        context.ServerRooms.Add(serverRoom);
        context.Agents.Add(agent);
        context.ShellyDevices.Add(shelly);
        await context.SaveChangesAsync();

        return (serverRoom, shelly);
    }

    private static async Task<(ServerRoom ServerRoom, MonitoredDevice MonitoredDevice)> SeedMonitoredDeviceScenarioAsync(SRMCore.Data.SrmCoreDbContext context)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Customer", ContactEmail = "customer@example.com", ContactPhone = "123", ExternalReference = "C1", IsActive = true };
        var serverRoom = new ServerRoom
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Name = "Room A",
            LocationDescription = "A",
            TemperatureWarningThreshold = 25,
            TemperatureCriticalThreshold = 30,
            MonitoringEnabled = true
        };
        var agent = new Agent { Id = Guid.NewGuid(), ServerRoomId = serverRoom.Id, Name = "Agent", ApiKeyReference = "ref", Version = "1.0", LastKnownIpAddress = "127.0.0.1", IsActive = true };
        var monitoredDevice = new MonitoredDevice { Id = Guid.NewGuid(), AgentId = agent.Id, DisplayName = "Switch", IpAddress = "192.168.1.10", IntervalSeconds = 30, TimeoutMilliseconds = 1000, FailureThreshold = 3, IsActive = true };

        context.Customers.Add(customer);
        context.ServerRooms.Add(serverRoom);
        context.Agents.Add(agent);
        context.MonitoredDevices.Add(monitoredDevice);
        await context.SaveChangesAsync();

        return (serverRoom, monitoredDevice);
    }
}

