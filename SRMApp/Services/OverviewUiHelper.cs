namespace SRMApp.Services;

using SRMShared.DTOs.Incident;
using SRMShared.Entities;

public static class OverviewUiHelper
{
    public static bool IsClosedIncident(IncidentReadDto incident)
    {
        var ticketStatus = incident.TicketLinks
            .FirstOrDefault(x => x.ProviderName == "Redmine")?.ExternalStatusName;
        if (!string.IsNullOrWhiteSpace(ticketStatus))
        {
            return ticketStatus is "Resolved" or "Closed" or "Rejected";
        }

        return incident.Status is IncidentStatus.Resolved or IncidentStatus.Closed or IncidentStatus.Rejected;
    }

    public static bool IsOpenIncident(IncidentReadDto incident)
        => !IsClosedIncident(incident);

    public static string FormatTemperatureCelsius(double value, int decimals = 1)
        => $"{Math.Round(value, decimals).ToString($"F{decimals}")} C";

    public static string FormatPercentage(double value, int decimals = 0)
        => $"{Math.Round(value, decimals):F0}%";

    public static string FormatPercentageWidth(double value)
        => $"{Math.Clamp(value, 0, 100):0}%";

    public static string FormatCountRatio(int current, int total)
        => $"{current} / {total}";

    public static string GetStatusTextClass(string stateClass)
        => stateClass switch
        {
            "alert-critical" => "status-line-critical",
            "alert-warning" => "status-line-warning",
            "alert-info" => "status-line-info",
            "alert-ok" => "status-line-ok",
            _ => "subtle"
        };

    public static string GetBadgeClass(string stateClass)
        => stateClass switch
        {
            "alert-critical" => "badge-critical",
            "alert-warning" => "badge-warning",
            "alert-info" => "badge-info",
            "alert-ok" => "badge-ok",
            _ => "badge-soft"
        };

    public static string GetSeverityCardClass(string? severity)
        => severity switch
        {
            "Critical" => "alert-critical",
            "Major" => "alert-warning",
            "Warning" => "alert-warning",
            "Information" => "alert-info",
            _ => "alert-ok"
        };

    public static string FormatLastLogLabel(DateTime? value, Func<string, string> translate)
        => value.HasValue
            ? $"{translate("LastLog")}: {FormatLocalDateTime(value.Value)}"
            : $"{translate("LastLog")}: {translate("NoData")}";

    public static string FormatLocalDateTime(DateTime value)
        => value.ToLocalTime().ToString("g");
}
