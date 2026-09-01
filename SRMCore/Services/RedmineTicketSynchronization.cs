using SRMCore.Configuration;
using SRMShared.Entities;

namespace SRMCore.Services;

public static class RedmineTicketSynchronization
{
    public static bool RepairPublicUrl(TicketLink ticketLink, RedmineOptions options)
    {
        var expectedUrl = options.BuildPublicIssueUrl(ticketLink.ExternalTicketId);
        if (string.Equals(ticketLink.ExternalTicketUrl, expectedUrl, StringComparison.Ordinal))
        {
            return false;
        }

        ticketLink.ExternalTicketUrl = expectedUrl;
        return true;
    }

    public static void ApplyIssueDetails(
        TicketLink ticketLink,
        RedmineIssueDetails details,
        DateTime synchronizedAtUtc)
    {
        ticketLink.ExternalStatusName = details.StatusName;
        ticketLink.ExternalPriorityName = details.PriorityName;
        ticketLink.ExternalDataSynchronizedAtUtc = synchronizedAtUtc;

        var incidentStatus = MapIncidentStatus(details.StatusName);
        if (ticketLink.Incident is null || !incidentStatus.HasValue)
        {
            return;
        }

        ticketLink.Incident.Status = incidentStatus.Value;
        ticketLink.Incident.UpdatedAtUtc = synchronizedAtUtc;
        ticketLink.Incident.ClosedAtUtc = incidentStatus is IncidentStatus.Closed or IncidentStatus.Rejected
            ? ticketLink.Incident.ClosedAtUtc ?? synchronizedAtUtc
            : null;
    }

    private static IncidentStatus? MapIncidentStatus(string externalStatusName)
    {
        return externalStatusName switch
        {
            "New" => IncidentStatus.New,
            "In Progress" => IncidentStatus.InProgress,
            "Resolved" => IncidentStatus.Resolved,
            "Feedback" => IncidentStatus.Feedback,
            "Closed" => IncidentStatus.Closed,
            "Rejected" => IncidentStatus.Rejected,
            _ => null
        };
    }
}
