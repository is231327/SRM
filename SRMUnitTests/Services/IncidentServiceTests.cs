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
        Assert.That(incident.Status, Is.EqualTo(IncidentStatus.New));
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
            Status = IncidentStatus.New,
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
        Assert.That(ticketLink.SyncStatus, Is.EqualTo(TicketSyncStatus.Created));
        Assert.That(ticketLink.PendingComment, Does.Contain("Door closed"));
        Assert.That(ticketLink.LastErrorMessage, Is.Empty);
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
    public async Task EvaluateSensorReadingAsync_ShouldNotRequeueCreatedTicket_ForRepeatedOpenCondition()
    {
        using var context = DbContextFactory.CreateContext();
        var data = await SeedShellyScenarioAsync(context);
        var service = new IncidentService(context, new TicketDispatchService(context));
        var firstReading = new SensorReading
        {
            ShellyDeviceId = data.ShellyDevice.Id,
            TemperatureCelsius = 20,
            DoorOpen = true,
            RecordedAtUtc = DateTime.UtcNow.AddSeconds(-10)
        };
        context.SensorReadings.Add(firstReading);
        await context.SaveChangesAsync();
        await service.EvaluateSensorReadingAsync(firstReading);

        var ticketLink = await context.TicketLinks.SingleAsync();
        ticketLink.SyncStatus = TicketSyncStatus.Created;
        ticketLink.ExternalTicketId = "123";
        await context.SaveChangesAsync();

        var repeatedReading = new SensorReading
        {
            ShellyDeviceId = data.ShellyDevice.Id,
            TemperatureCelsius = 20,
            DoorOpen = true,
            RecordedAtUtc = DateTime.UtcNow
        };
        context.SensorReadings.Add(repeatedReading);
        await context.SaveChangesAsync();
        await service.EvaluateSensorReadingAsync(repeatedReading);

        Assert.That(await context.TicketLinks.CountAsync(), Is.EqualTo(1));
        Assert.That(ticketLink.SyncStatus, Is.EqualTo(TicketSyncStatus.Created));
        Assert.That(ticketLink.ExternalTicketId, Is.EqualTo("123"));
    }

    [Test]
    public async Task EvaluateSensorReadingAsync_ShouldCreateNewDoorIncident_WhenDoorReopensAfterClosing()
    {
        using var context = DbContextFactory.CreateContext();
        var data = await SeedShellyScenarioAsync(context);
        var service = new IncidentService(context, new TicketDispatchService(context));
        var openedAt = DateTime.UtcNow.AddMinutes(-2);

        await AddAndEvaluateReadingAsync(context, service, data.ShellyDevice.Id, doorOpen: true, temperature: 20, openedAt);
        var originalIncident = await context.Incidents.SingleAsync();
        var originalTicket = await context.TicketLinks.SingleAsync();
        originalTicket.ExternalTicketId = "11";
        originalTicket.ExternalStatusName = "New";
        originalTicket.SyncStatus = TicketSyncStatus.Created;
        await context.SaveChangesAsync();

        await AddAndEvaluateReadingAsync(context, service, data.ShellyDevice.Id, doorOpen: false, temperature: 20, openedAt.AddMinutes(1));

        // The Redmine synchronization maps the still-open ticket status back to New.
        originalIncident.Status = IncidentStatus.New;
        await context.SaveChangesAsync();
        await AddAndEvaluateReadingAsync(context, service, data.ShellyDevice.Id, doorOpen: true, temperature: 20, openedAt.AddMinutes(2));

        var incidents = await context.Incidents.OrderBy(x => x.OpenedAtUtc).ToListAsync();
        var tickets = await context.TicketLinks.OrderBy(x => x.CreatedAtUtc).ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(incidents, Has.Count.EqualTo(2));
            Assert.That(incidents[0].ResolvedAtUtc, Is.Not.Null);
            Assert.That(incidents[1].ResolvedAtUtc, Is.Null);
            Assert.That(tickets, Has.Count.EqualTo(2));
            Assert.That(tickets[0].PendingComment, Does.Contain("Door closed"));
            Assert.That(tickets[1].SyncStatus, Is.EqualTo(TicketSyncStatus.PendingCreate));
        });
    }

    [Test]
    public async Task EvaluateSensorReadingAsync_ShouldReuseTemperatureTicketAndQueuePriorityUpdate_WhenSeverityChanges()
    {
        using var context = DbContextFactory.CreateContext();
        var data = await SeedShellyScenarioAsync(context);
        var service = new IncidentService(context, new TicketDispatchService(context));

        var warningReading = new SensorReading
        {
            ShellyDeviceId = data.ShellyDevice.Id,
            TemperatureCelsius = 26,
            DoorOpen = false,
            RecordedAtUtc = DateTime.UtcNow.AddSeconds(-10)
        };
        context.SensorReadings.Add(warningReading);
        await context.SaveChangesAsync();
        await service.EvaluateSensorReadingAsync(warningReading);

        var originalIncident = await context.Incidents.SingleAsync();
        var ticketLink = await context.TicketLinks.SingleAsync();
        ticketLink.ExternalTicketId = "7";
        ticketLink.SyncStatus = TicketSyncStatus.Created;
        await context.SaveChangesAsync();

        var criticalReading = new SensorReading
        {
            ShellyDeviceId = data.ShellyDevice.Id,
            TemperatureCelsius = 31,
            DoorOpen = false,
            RecordedAtUtc = DateTime.UtcNow
        };
        context.SensorReadings.Add(criticalReading);
        await context.SaveChangesAsync();
        await service.EvaluateSensorReadingAsync(criticalReading);

        var updatedIncident = await context.Incidents.SingleAsync();
        var updatedTicketLink = await context.TicketLinks.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(updatedIncident.Id, Is.EqualTo(originalIncident.Id));
            Assert.That(updatedIncident.Type, Is.EqualTo(IncidentType.TemperatureCriticalThresholdExceeded));
            Assert.That(updatedIncident.Severity, Is.EqualTo(IncidentSeverity.Critical));
            Assert.That(updatedTicketLink.ExternalTicketId, Is.EqualTo("7"));
            Assert.That(updatedTicketLink.PriorityUpdatePending, Is.True);
            Assert.That(updatedTicketLink.SyncStatus, Is.EqualTo(TicketSyncStatus.Created));
        });
    }

    [Test]
    public async Task EvaluateSensorReadingAsync_ShouldReuseOpenTemperatureTicket_AfterConditionResolvedAndRecurred()
    {
        using var context = DbContextFactory.CreateContext();
        var data = await SeedShellyScenarioAsync(context);
        var service = new IncidentService(context, new TicketDispatchService(context));
        var startedAt = DateTime.UtcNow.AddMinutes(-2);

        await AddAndEvaluateReadingAsync(context, service, data.ShellyDevice.Id, doorOpen: false, temperature: 26, startedAt);
        var originalIncident = await context.Incidents.SingleAsync();
        var ticket = await context.TicketLinks.SingleAsync();
        ticket.ExternalTicketId = "12";
        ticket.ExternalStatusName = "New";
        ticket.SyncStatus = TicketSyncStatus.Created;
        await context.SaveChangesAsync();

        await AddAndEvaluateReadingAsync(context, service, data.ShellyDevice.Id, doorOpen: false, temperature: 20, startedAt.AddMinutes(1));
        originalIncident.Status = IncidentStatus.New;
        await context.SaveChangesAsync();
        await AddAndEvaluateReadingAsync(context, service, data.ShellyDevice.Id, doorOpen: false, temperature: 31, startedAt.AddMinutes(2));

        var incident = await context.Incidents.SingleAsync();
        var updatedTicket = await context.TicketLinks.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(incident.Id, Is.EqualTo(originalIncident.Id));
            Assert.That(incident.Type, Is.EqualTo(IncidentType.TemperatureCriticalThresholdExceeded));
            Assert.That(incident.ResolvedAtUtc, Is.Null);
            Assert.That(updatedTicket.ExternalTicketId, Is.EqualTo("12"));
            Assert.That(updatedTicket.PendingComment, Does.Contain("Temperature returned to normal"));
            Assert.That(updatedTicket.PriorityUpdatePending, Is.True);
        });
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

    private static async Task AddAndEvaluateReadingAsync(
        SRMCore.Data.SrmCoreDbContext context,
        IncidentService service,
        Guid shellyDeviceId,
        bool doorOpen,
        float temperature,
        DateTime recordedAtUtc)
    {
        var reading = new SensorReading
        {
            ShellyDeviceId = shellyDeviceId,
            TemperatureCelsius = temperature,
            DoorOpen = doorOpen,
            RecordedAtUtc = recordedAtUtc
        };
        context.SensorReadings.Add(reading);
        await context.SaveChangesAsync();
        await service.EvaluateSensorReadingAsync(reading);
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

