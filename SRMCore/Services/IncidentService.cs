using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class IncidentService(
    SrmCoreDbContext dbContext,
    ITicketDispatchService ticketDispatchService) : IIncidentService
{
    private static readonly string[] TerminalTicketStatuses = ["Resolved", "Rejected", "Closed"];
    private static readonly IncidentStatus[] ActiveIncidentStatuses =
        [IncidentStatus.New, IncidentStatus.InProgress, IncidentStatus.Feedback];

    public async Task EvaluateSensorReadingAsync(SensorReading sensorReading, CancellationToken cancellationToken = default)
    {
        var context = await dbContext.SensorReadings
            .Where(x => x.Id == sensorReading.Id)
            .Select(x => new
            {
                SensorReading = x,
                ShellyDevice = x.ShellyDevice!,
                Agent = x.ShellyDevice!.Agent!,
                ServerRoom = x.ShellyDevice!.Agent!.ServerRoom!,
                Customer = x.ShellyDevice!.Agent!.ServerRoom!.Customer!
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (context is null)
        {
            return;
        }

        var activeMaintenanceWindow = await dbContext.MaintenanceWindows.AnyAsync(x =>
                x.ServerRoomId == context.ServerRoom.Id
                && x.StartUtc <= context.SensorReading.RecordedAtUtc
                && x.EndUtc >= context.SensorReading.RecordedAtUtc,
            cancellationToken);

        await HandleDoorIncidentAsync(context.SensorReading, context.ServerRoom, context.ShellyDevice, activeMaintenanceWindow, cancellationToken);
        await HandleTemperatureIncidentAsync(context.SensorReading, context.ServerRoom, context.ShellyDevice, cancellationToken);
    }

    public async Task EvaluatePingResultAsync(MonitoredDevicePingResult pingResult, CancellationToken cancellationToken = default)
    {
        var context = await dbContext.MonitoredDevicePingResults
            .Where(x => x.Id == pingResult.Id)
            .Select(x => new
            {
                PingResult = x,
                MonitoredDevice = x.MonitoredDevice!,
                Agent = x.MonitoredDevice!.Agent!,
                ServerRoom = x.MonitoredDevice!.Agent!.ServerRoom!
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (context is null)
        {
            return;
        }

        var correlationKey = BuildCorrelationKey(IncidentType.MonitoredDeviceFailureThresholdReached, context.ServerRoom.Id, null, context.MonitoredDevice.Id);
        var summary = $"Monitored device {context.MonitoredDevice.DisplayName} is unreachable";
        var description = $"Device {context.MonitoredDevice.DisplayName} ({context.MonitoredDevice.IpAddress}) reached failure threshold {context.MonitoredDevice.FailureThreshold}.";

        if (context.PingResult.FailureThresholdReached && !context.PingResult.IsReachable)
        {
            var incident = await OpenOrUpdateIncidentAsync(
                IncidentType.MonitoredDeviceFailureThresholdReached,
                IncidentSeverity.Major,
                context.ServerRoom.Id,
                null,
                context.MonitoredDevice.Id,
                correlationKey,
                summary,
                description,
                context.PingResult.RecordedAtUtc,
                cancellationToken);

            await AppendIncidentEventAsync(incident.Id, "Trigger", $"Failure threshold reached for monitored device {context.MonitoredDevice.DisplayName}.", context.PingResult.RecordedAtUtc, cancellationToken);
            await ticketDispatchService.QueueCreateAsync(incident, cancellationToken);
            return;
        }

        if (context.PingResult.IsReachable)
        {
            var incident = await FindOpenIncidentAsync(correlationKey, cancellationToken);
            if (incident is null)
            {
                return;
            }

            incident.Status = IncidentStatus.Resolved;
            incident.ResolvedAtUtc = context.PingResult.RecordedAtUtc;
            incident.LastOccurredAtUtc = context.PingResult.RecordedAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            await AppendIncidentEventAsync(incident.Id, "Resolved", $"Monitored device {context.MonitoredDevice.DisplayName} became reachable again.", context.PingResult.RecordedAtUtc, cancellationToken);
            await ticketDispatchService.QueueResolutionCommentAsync(incident, $"Condition cleared at {context.PingResult.RecordedAtUtc:O}.", cancellationToken);
        }
    }

    private async Task HandleDoorIncidentAsync(
        SensorReading sensorReading,
        ServerRoom serverRoom,
        ShellyDevice shellyDevice,
        bool activeMaintenanceWindow,
        CancellationToken cancellationToken)
    {
        var correlationKey = BuildCorrelationKey(IncidentType.DoorOpenOutsideMaintenanceWindow, serverRoom.Id, shellyDevice.Id, null);

        if (sensorReading.DoorOpen && !activeMaintenanceWindow)
        {
            var incident = await OpenOrUpdateIncidentAsync(
                IncidentType.DoorOpenOutsideMaintenanceWindow,
                IncidentSeverity.Critical,
                serverRoom.Id,
                shellyDevice.Id,
                null,
                correlationKey,
                $"Door opened outside maintenance window in server room {serverRoom.Name}",
                $"Shelly device {shellyDevice.Name} reported an open door outside an active maintenance window.",
                sensorReading.RecordedAtUtc,
                cancellationToken);

            await AppendIncidentEventAsync(incident.Id, "Trigger", $"Door opened outside maintenance window for Shelly device {shellyDevice.Name}.", sensorReading.RecordedAtUtc, cancellationToken);
            await ticketDispatchService.QueueCreateAsync(incident, cancellationToken);
            return;
        }

        if (!sensorReading.DoorOpen)
        {
            var incident = await FindOpenIncidentAsync(correlationKey, cancellationToken);
            if (incident is null)
            {
                return;
            }

            incident.Status = IncidentStatus.Resolved;
            incident.ResolvedAtUtc = sensorReading.RecordedAtUtc;
            incident.LastOccurredAtUtc = sensorReading.RecordedAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            await AppendIncidentEventAsync(incident.Id, "Resolved", $"Door closed again for Shelly device {shellyDevice.Name}.", sensorReading.RecordedAtUtc, cancellationToken);
            await ticketDispatchService.QueueResolutionCommentAsync(incident, $"Door closed at {sensorReading.RecordedAtUtc:O}.", cancellationToken);
        }
    }

    private async Task HandleTemperatureIncidentAsync(
        SensorReading sensorReading,
        ServerRoom serverRoom,
        ShellyDevice shellyDevice,
        CancellationToken cancellationToken)
    {
        var correlationKey = $"Temperature:{serverRoom.Id}";

        if (sensorReading.TemperatureCelsius >= serverRoom.TemperatureCriticalThreshold)
        {
            await OpenOrUpdateTemperatureIncidentAsync(
                sensorReading,
                serverRoom,
                shellyDevice,
                IncidentType.TemperatureCriticalThresholdExceeded,
                IncidentSeverity.Critical,
                $"Critical temperature detected in server room {serverRoom.Name}",
                $"Shelly device {shellyDevice.Name} reported {sensorReading.TemperatureCelsius} C, exceeding the critical threshold of {serverRoom.TemperatureCriticalThreshold} C.",
                correlationKey,
                cancellationToken);
            return;
        }

        if (sensorReading.TemperatureCelsius >= serverRoom.TemperatureWarningThreshold)
        {
            await OpenOrUpdateTemperatureIncidentAsync(
                sensorReading,
                serverRoom,
                shellyDevice,
                IncidentType.TemperatureWarningThresholdExceeded,
                IncidentSeverity.Warning,
                $"Temperature warning detected in server room {serverRoom.Name}",
                $"Shelly device {shellyDevice.Name} reported {sensorReading.TemperatureCelsius} C, exceeding the warning threshold of {serverRoom.TemperatureWarningThreshold} C.",
                correlationKey,
                cancellationToken);
            return;
        }

        var openIncident = await FindOpenIncidentAsync(correlationKey, cancellationToken);
        if (openIncident is not null)
        {
            openIncident.Status = IncidentStatus.Resolved;
            openIncident.ResolvedAtUtc = sensorReading.RecordedAtUtc;
            openIncident.LastOccurredAtUtc = sensorReading.RecordedAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            await AppendIncidentEventAsync(openIncident.Id, "Resolved", $"Temperature returned to normal at {sensorReading.TemperatureCelsius} C.", sensorReading.RecordedAtUtc, cancellationToken);
            await ticketDispatchService.QueueResolutionCommentAsync(openIncident, $"Temperature returned to normal at {sensorReading.RecordedAtUtc:O}.", cancellationToken);
        }
    }

    private async Task OpenOrUpdateTemperatureIncidentAsync(
        SensorReading sensorReading,
        ServerRoom serverRoom,
        ShellyDevice shellyDevice,
        IncidentType type,
        IncidentSeverity severity,
        string summary,
        string description,
        string correlationKey,
        CancellationToken cancellationToken)
    {
        var incident = await FindReusableTemperatureIncidentAsync(correlationKey, cancellationToken);
        var isNew = incident is null;
        var severityChanged = incident is not null && incident.Severity != severity;

        if (incident is null)
        {
            incident = new Incident
            {
                ServerRoomId = serverRoom.Id,
                ShellyDeviceId = shellyDevice.Id,
                Type = type,
                Severity = severity,
                Status = IncidentStatus.New,
                CorrelationKey = correlationKey,
                Summary = summary,
                Description = description,
                OpenedAtUtc = sensorReading.RecordedAtUtc,
                LastOccurredAtUtc = sensorReading.RecordedAtUtc
            };
            dbContext.Incidents.Add(incident);
        }
        else
        {
            incident.ShellyDeviceId = shellyDevice.Id;
            incident.Type = type;
            incident.Severity = severity;
            incident.Status = GetActiveStatus(incident);
            incident.ResolvedAtUtc = null;
            incident.ClosedAtUtc = null;
            incident.Summary = summary;
            incident.Description = description;
            incident.LastOccurredAtUtc = sensorReading.RecordedAtUtc;
            incident.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var eventType = severityChanged ? "PriorityChanged" : "Trigger";
        await AppendIncidentEventAsync(
            incident.Id,
            eventType,
            $"{severity} temperature {sensorReading.TemperatureCelsius} C reported by {shellyDevice.Name}.",
            sensorReading.RecordedAtUtc,
            cancellationToken);

        await ticketDispatchService.QueueCreateAsync(incident, cancellationToken);
        if (!isNew && severityChanged)
        {
            await ticketDispatchService.QueuePriorityUpdateAsync(incident, cancellationToken);
        }
    }

    private async Task<Incident?> FindReusableTemperatureIncidentAsync(
        string correlationKey,
        CancellationToken cancellationToken)
    {
        var openIncident = await dbContext.Incidents
            .Include(x => x.TicketLinks)
            .FirstOrDefaultAsync(
                x => x.CorrelationKey == correlationKey && ActiveIncidentStatuses.Contains(x.Status),
                cancellationToken);

        if (openIncident is not null
            && !openIncident.TicketLinks.Any(x => TerminalTicketStatuses.Contains(x.ExternalStatusName)))
        {
            return openIncident;
        }

        if (openIncident is not null)
        {
            openIncident.Status = IncidentStatus.Closed;
            openIncident.ClosedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await dbContext.Incidents
            .Include(x => x.TicketLinks)
            .Where(x => x.CorrelationKey == correlationKey && x.Status == IncidentStatus.Resolved)
            .Where(x => !x.TicketLinks.Any(t => TerminalTicketStatuses.Contains(t.ExternalStatusName)))
            .OrderByDescending(x => x.LastOccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Incident> OpenOrUpdateIncidentAsync(
        IncidentType type,
        IncidentSeverity severity,
        Guid serverRoomId,
        Guid? shellyDeviceId,
        Guid? monitoredDeviceId,
        string correlationKey,
        string summary,
        string description,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var incident = await FindOpenIncidentAsync(correlationKey, cancellationToken);
        if (incident is null)
        {
            incident = new Incident
            {
                ServerRoomId = serverRoomId,
                ShellyDeviceId = shellyDeviceId,
                MonitoredDeviceId = monitoredDeviceId,
                Type = type,
                Severity = severity,
                Status = IncidentStatus.New,
                CorrelationKey = correlationKey,
                Summary = summary,
                Description = description,
                OpenedAtUtc = occurredAtUtc,
                LastOccurredAtUtc = occurredAtUtc
            };

            dbContext.Incidents.Add(incident);
        }
        else
        {
            incident.Severity = severity;
            incident.Summary = summary;
            incident.Description = description;
            incident.LastOccurredAtUtc = occurredAtUtc;
            incident.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return incident;
    }

    private Task<Incident?> FindOpenIncidentAsync(string correlationKey, CancellationToken cancellationToken)
    {
        return dbContext.Incidents
            .FirstOrDefaultAsync(
                x => x.CorrelationKey == correlationKey
                    && ActiveIncidentStatuses.Contains(x.Status)
                    && !x.ResolvedAtUtc.HasValue
                    && !x.ClosedAtUtc.HasValue,
                cancellationToken);
    }

    private async Task AppendIncidentEventAsync(Guid incidentId, string eventType, string message, DateTime occurredAtUtc, CancellationToken cancellationToken)
    {
        dbContext.IncidentEvents.Add(new IncidentEvent
        {
            IncidentId = incidentId,
            EventType = eventType,
            Message = message,
            OccurredAtUtc = occurredAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IncidentStatus GetActiveStatus(Incident incident)
    {
        var externalStatus = incident.TicketLinks
            .Select(x => x.ExternalStatusName)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        return externalStatus switch
        {
            "In Progress" => IncidentStatus.InProgress,
            "Feedback" => IncidentStatus.Feedback,
            _ => IncidentStatus.New
        };
    }

    private static string BuildCorrelationKey(IncidentType type, Guid serverRoomId, Guid? shellyDeviceId, Guid? monitoredDeviceId)
    {
        return $"{type}:{serverRoomId}:{shellyDeviceId?.ToString() ?? "none"}:{monitoredDeviceId?.ToString() ?? "none"}";
    }
}
