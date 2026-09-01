using SRMShared.DTOs.Incident;
using SRMShared.Entities;

namespace SRMCore.Mappings;

public static class IncidentReadDtoMapper
{
    public static IncidentReadDto ToReadDto(Incident entity)
    {
        return new IncidentReadDto
        {
            Id = entity.Id,
            ServerRoomId = entity.ServerRoomId,
            ServerRoomName = entity.ServerRoom?.Name ?? string.Empty,
            ShellyDeviceId = entity.ShellyDeviceId,
            ShellyDeviceName = entity.ShellyDevice?.Name ?? string.Empty,
            MonitoredDeviceId = entity.MonitoredDeviceId,
            MonitoredDeviceName = entity.MonitoredDevice?.DisplayName ?? string.Empty,
            Type = entity.Type,
            Severity = entity.Severity,
            Status = entity.Status,
            CorrelationKey = entity.CorrelationKey,
            Summary = entity.Summary,
            Description = entity.Description,
            OpenedAtUtc = entity.OpenedAtUtc,
            ResolvedAtUtc = entity.ResolvedAtUtc,
            ClosedAtUtc = entity.ClosedAtUtc,
            LastOccurredAtUtc = entity.LastOccurredAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Events = entity.Events
                .OrderByDescending(x => x.OccurredAtUtc)
                .Select(x => new IncidentEventReadDto
                {
                    Id = x.Id,
                    IncidentId = x.IncidentId,
                    EventType = x.EventType,
                    Message = x.Message,
                    OccurredAtUtc = x.OccurredAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc
                })
                .ToList(),
            TicketLinks = entity.TicketLinks
                .OrderByDescending(x => x.UpdatedAtUtc)
                .Select(x => new TicketLinkReadDto
                {
                    Id = x.Id,
                    IncidentId = x.IncidentId,
                    ProviderName = x.ProviderName,
                    ExternalTicketId = x.ExternalTicketId,
                    ExternalTicketUrl = x.ExternalTicketUrl,
                    ExternalStatusName = x.ExternalStatusName,
                    ExternalPriorityName = x.ExternalPriorityName,
                    ExternalDataSynchronizedAtUtc = x.ExternalDataSynchronizedAtUtc,
                    SyncStatus = x.SyncStatus,
                    LastErrorMessage = x.LastErrorMessage,
                    PendingComment = x.PendingComment,
                    LastSyncAttemptAtUtc = x.LastSyncAttemptAtUtc,
                    SyncAttemptCount = x.SyncAttemptCount,
                    NextSyncAttemptAtUtc = x.NextSyncAttemptAtUtc,
                    CreatedInExternalSystemAtUtc = x.CreatedInExternalSystemAtUtc,
                    LastCommentedAtUtc = x.LastCommentedAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc
                })
                .ToList()
        };
    }
}
