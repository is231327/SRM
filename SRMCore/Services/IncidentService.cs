using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class IncidentService(
    SrmCoreDbContext dbContext,
    ITicketDispatchService ticketDispatchService) : IIncidentService
{
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
        var warningKey = BuildCorrelationKey(IncidentType.TemperatureWarningThresholdExceeded, serverRoom.Id, shellyDevice.Id, null);
        var criticalKey = BuildCorrelationKey(IncidentType.TemperatureCriticalThresholdExceeded, serverRoom.Id, shellyDevice.Id, null);

        if (sensorReading.TemperatureCelsius >= serverRoom.TemperatureCriticalThreshold)
        {
            var warningIncident = await FindOpenIncidentAsync(warningKey, cancellationToken);
            if (warningIncident is not null)
            {
                warningIncident.Status = IncidentStatus.Resolved;
                warningIncident.ResolvedAtUtc = sensorReading.RecordedAtUtc;
            }

            var incident = await OpenOrUpdateIncidentAsync(
                IncidentType.TemperatureCriticalThresholdExceeded,
                IncidentSeverity.Critical,
                serverRoom.Id,
                shellyDevice.Id,
                null,
                criticalKey,
                $"Critical temperature detected in server room {serverRoom.Name}",
                $"Shelly device {shellyDevice.Name} reported {sensorReading.TemperatureCelsius} C, exceeding the critical threshold of {serverRoom.TemperatureCriticalThreshold} C.",
                sensorReading.RecordedAtUtc,
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            await AppendIncidentEventAsync(incident.Id, "Trigger", $"Critical temperature {sensorReading.TemperatureCelsius} C reported by {shellyDevice.Name}.", sensorReading.RecordedAtUtc, cancellationToken);
            await ticketDispatchService.QueueCreateAsync(incident, cancellationToken);
            return;
        }

        if (sensorReading.TemperatureCelsius >= serverRoom.TemperatureWarningThreshold)
        {
            var criticalIncident = await FindOpenIncidentAsync(criticalKey, cancellationToken);
            if (criticalIncident is not null)
            {
                criticalIncident.Status = IncidentStatus.Resolved;
                criticalIncident.ResolvedAtUtc = sensorReading.RecordedAtUtc;
                await dbContext.SaveChangesAsync(cancellationToken);
                await AppendIncidentEventAsync(criticalIncident.Id, "Resolved", $"Temperature fell below critical threshold at {sensorReading.TemperatureCelsius} C.", sensorReading.RecordedAtUtc, cancellationToken);
                await ticketDispatchService.QueueResolutionCommentAsync(criticalIncident, $"Temperature fell below critical threshold at {sensorReading.RecordedAtUtc:O}.", cancellationToken);
            }

            var incident = await OpenOrUpdateIncidentAsync(
                IncidentType.TemperatureWarningThresholdExceeded,
                IncidentSeverity.Warning,
                serverRoom.Id,
                shellyDevice.Id,
                null,
                warningKey,
                $"Temperature warning detected in server room {serverRoom.Name}",
                $"Shelly device {shellyDevice.Name} reported {sensorReading.TemperatureCelsius} C, exceeding the warning threshold of {serverRoom.TemperatureWarningThreshold} C.",
                sensorReading.RecordedAtUtc,
                cancellationToken);

            await AppendIncidentEventAsync(incident.Id, "Trigger", $"Warning temperature {sensorReading.TemperatureCelsius} C reported by {shellyDevice.Name}.", sensorReading.RecordedAtUtc, cancellationToken);
            await ticketDispatchService.QueueCreateAsync(incident, cancellationToken);
            return;
        }

        var warningOpen = await FindOpenIncidentAsync(warningKey, cancellationToken);
        if (warningOpen is not null)
        {
            warningOpen.Status = IncidentStatus.Resolved;
            warningOpen.ResolvedAtUtc = sensorReading.RecordedAtUtc;
            warningOpen.LastOccurredAtUtc = sensorReading.RecordedAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            await AppendIncidentEventAsync(warningOpen.Id, "Resolved", $"Temperature returned to normal at {sensorReading.TemperatureCelsius} C.", sensorReading.RecordedAtUtc, cancellationToken);
            await ticketDispatchService.QueueResolutionCommentAsync(warningOpen, $"Temperature returned to normal at {sensorReading.RecordedAtUtc:O}.", cancellationToken);
        }

        var criticalOpen = await FindOpenIncidentAsync(criticalKey, cancellationToken);
        if (criticalOpen is not null)
        {
            criticalOpen.Status = IncidentStatus.Resolved;
            criticalOpen.ResolvedAtUtc = sensorReading.RecordedAtUtc;
            criticalOpen.LastOccurredAtUtc = sensorReading.RecordedAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            await AppendIncidentEventAsync(criticalOpen.Id, "Resolved", $"Temperature returned to normal at {sensorReading.TemperatureCelsius} C.", sensorReading.RecordedAtUtc, cancellationToken);
            await ticketDispatchService.QueueResolutionCommentAsync(criticalOpen, $"Temperature returned to normal at {sensorReading.RecordedAtUtc:O}.", cancellationToken);
        }
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
                Status = IncidentStatus.Open,
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
            .FirstOrDefaultAsync(x => x.CorrelationKey == correlationKey && x.Status == IncidentStatus.Open, cancellationToken);
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

    private static string BuildCorrelationKey(IncidentType type, Guid serverRoomId, Guid? shellyDeviceId, Guid? monitoredDeviceId)
    {
        return $"{type}:{serverRoomId}:{shellyDeviceId?.ToString() ?? "none"}:{monitoredDeviceId?.ToString() ?? "none"}";
    }
}
