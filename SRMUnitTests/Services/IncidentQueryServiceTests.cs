using Microsoft.EntityFrameworkCore;
using SRMCore.Services;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Services;

[TestFixture]
public class IncidentQueryServiceTests
{
    [Test]
    public async Task GetAllAsync_HidesIncidentsWithTerminalRedmineStatus()
    {
        using var context = DbContextFactory.CreateContext();
        var customer = new Customer
        {
            Name = "Customer",
            ExternalReference = "C1",
            ContactEmail = "customer@example.com",
            ContactPhone = "123",
            IsActive = true
        };
        var serverRoom = new ServerRoom
        {
            CustomerId = customer.Id,
            Name = "Room",
            LocationDescription = "Location",
            TemperatureWarningThreshold = 25,
            TemperatureCriticalThreshold = 30,
            MonitoringEnabled = true
        };
        context.AddRange(customer, serverRoom);

        foreach (var externalStatus in new[] { "In Progress", "Resolved", "Rejected", "Closed" })
        {
            var incident = new Incident
            {
                ServerRoomId = serverRoom.Id,
                Type = IncidentType.TemperatureWarningThresholdExceeded,
                Severity = IncidentSeverity.Warning,
                Status = externalStatus == "In Progress" ? IncidentStatus.InProgress : IncidentStatus.Resolved,
                CorrelationKey = externalStatus,
                Summary = externalStatus,
                Description = externalStatus
            };
            context.Incidents.Add(incident);
            context.TicketLinks.Add(new TicketLink
            {
                IncidentId = incident.Id,
                ProviderName = "Redmine",
                ExternalTicketId = Guid.NewGuid().ToString(),
                ExternalStatusName = externalStatus,
                SyncStatus = TicketSyncStatus.Created
            });
        }

        await context.SaveChangesAsync();
        var service = new IncidentQueryService(context, CoreCurrentUserContextFactory.Create());

        var result = await service.GetAllAsync();

        Assert.That(result.Select(x => x.Summary), Is.EqualTo(new[] { "In Progress" }));
    }

    [Test]
    public async Task GetAllAsync_IncludesTerminalRedmineStatusesWhenRequested()
    {
        using var context = DbContextFactory.CreateContext();
        var customer = new Customer { Name = "Customer", ExternalReference = "C2", ContactEmail = "customer@example.com", ContactPhone = "123", IsActive = true };
        var room = new ServerRoom { CustomerId = customer.Id, Name = "Room", LocationDescription = "Location", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true };
        var incident = new Incident { ServerRoomId = room.Id, Type = IncidentType.TemperatureWarningThresholdExceeded, Severity = IncidentSeverity.Warning, Status = IncidentStatus.Closed, CorrelationKey = "closed", Summary = "Closed", Description = "Closed" };
        context.AddRange(customer, room, incident, new TicketLink { IncidentId = incident.Id, ProviderName = "Redmine", ExternalTicketId = "7", ExternalStatusName = "Closed", SyncStatus = TicketSyncStatus.Created });
        await context.SaveChangesAsync();

        var service = new IncidentQueryService(context, CoreCurrentUserContextFactory.Create());
        var result = await service.GetAllAsync(includeClosed: true);

        Assert.That(result.Select(x => x.Summary), Is.EqualTo(new[] { "Closed" }));
    }
}
