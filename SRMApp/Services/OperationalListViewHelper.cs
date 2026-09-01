using SRMShared.DTOs.Incident;
using SRMShared.DTOs.MonitoredDevicePingResult;
using SRMShared.DTOs.SensorReading;
using SRMShared.Entities;

namespace SRMApp.Services;

public static class OperationalListViewHelper
{
    public static IReadOnlyList<IncidentReadDto> FilterAndSortIncidents(
        IEnumerable<IncidentReadDto> source,
        string searchText,
        IncidentType? type,
        string ticketPriority,
        string ticketStatus,
        IncidentSortOption sort)
    {
        var query = source
            .Where(x => MatchesIncidentSearch(x, searchText))
            .Where(x => !type.HasValue || x.Type == type.Value)
            .Where(x => string.IsNullOrWhiteSpace(ticketPriority)
                || string.Equals(GetTicketPriority(x), ticketPriority, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(ticketStatus)
                || string.Equals(GetTicketStatus(x), ticketStatus, StringComparison.OrdinalIgnoreCase));

        return sort switch
        {
            IncidentSortOption.DateOldest => query.OrderBy(GetIncidentDate).ToList(),
            IncidentSortOption.PriorityHighest => query.OrderByDescending(x => GetPriorityRank(GetTicketPriority(x))).ThenByDescending(GetIncidentDate).ToList(),
            IncidentSortOption.PriorityLowest => query.OrderBy(x => GetPriorityRank(GetTicketPriority(x))).ThenByDescending(GetIncidentDate).ToList(),
            IncidentSortOption.Status => query.OrderBy(x => GetStatusRank(GetTicketStatus(x))).ThenByDescending(GetIncidentDate).ToList(),
            IncidentSortOption.ServerRoomDescending => query.OrderByDescending(x => x.ServerRoomName, StringComparer.OrdinalIgnoreCase).ThenByDescending(GetIncidentDate).ToList(),
            IncidentSortOption.ServerRoomAscending => query.OrderBy(x => x.ServerRoomName, StringComparer.OrdinalIgnoreCase).ThenByDescending(GetIncidentDate).ToList(),
            _ => query.OrderByDescending(GetIncidentDate).ToList()
        };
    }

    public static IReadOnlyList<MonitoredDevicePingResultReadDto> FilterAndSortPingResults(
        IEnumerable<MonitoredDevicePingResultReadDto> source,
        TernaryFilter reachability,
        TernaryFilter thresholdReached,
        PingResultSortOption sort)
    {
        var query = source
            .Where(x => Matches(reachability, x.IsReachable))
            .Where(x => Matches(thresholdReached, x.FailureThresholdReached));

        return sort switch
        {
            PingResultSortOption.RecordedOldest => query.OrderBy(x => x.RecordedAtUtc).ToList(),
            PingResultSortOption.RoundtripFastest => query.OrderBy(x => x.RoundtripTimeMilliseconds).ThenByDescending(x => x.RecordedAtUtc).ToList(),
            PingResultSortOption.RoundtripSlowest => query.OrderByDescending(x => x.RoundtripTimeMilliseconds).ThenByDescending(x => x.RecordedAtUtc).ToList(),
            PingResultSortOption.FailureCountHighest => query.OrderByDescending(x => x.ConsecutiveFailureCount).ThenByDescending(x => x.RecordedAtUtc).ToList(),
            PingResultSortOption.FailureCountLowest => query.OrderBy(x => x.ConsecutiveFailureCount).ThenByDescending(x => x.RecordedAtUtc).ToList(),
            _ => query.OrderByDescending(x => x.RecordedAtUtc).ToList()
        };
    }

    public static IReadOnlyList<SensorReadingReadDto> FilterAndSortSensorReadings(
        IEnumerable<SensorReadingReadDto> source,
        TernaryFilter doorOpen,
        SensorReadingSortOption sort)
    {
        var query = source.Where(x => Matches(doorOpen, x.DoorOpen));

        return sort switch
        {
            SensorReadingSortOption.RecordedOldest => query.OrderBy(x => x.RecordedAtUtc).ToList(),
            SensorReadingSortOption.TemperatureHighest => query.OrderByDescending(x => x.TemperatureCelsius).ThenByDescending(x => x.RecordedAtUtc).ToList(),
            SensorReadingSortOption.TemperatureLowest => query.OrderBy(x => x.TemperatureCelsius).ThenByDescending(x => x.RecordedAtUtc).ToList(),
            SensorReadingSortOption.BatteryHighest => query.OrderByDescending(x => x.BatteryPercent).ThenByDescending(x => x.RecordedAtUtc).ToList(),
            SensorReadingSortOption.BatteryLowest => query.OrderBy(x => x.BatteryPercent).ThenByDescending(x => x.RecordedAtUtc).ToList(),
            SensorReadingSortOption.BrightnessHighest => query.OrderByDescending(x => x.Brightness).ThenByDescending(x => x.RecordedAtUtc).ToList(),
            SensorReadingSortOption.BrightnessLowest => query.OrderBy(x => x.Brightness).ThenByDescending(x => x.RecordedAtUtc).ToList(),
            _ => query.OrderByDescending(x => x.RecordedAtUtc).ToList()
        };
    }

    public static string GetTicketPriority(IncidentReadDto incident)
    {
        var externalPriority = GetRedmineTicketLink(incident)?.ExternalPriorityName;
        if (!string.IsNullOrWhiteSpace(externalPriority))
        {
            return externalPriority;
        }

        return incident.Severity switch
        {
            IncidentSeverity.Warning => "High",
            IncidentSeverity.Major => "Urgent",
            IncidentSeverity.Critical => "Immediate",
            _ => string.Empty
        };
    }

    public static string GetTicketStatus(IncidentReadDto incident)
        => GetRedmineTicketLink(incident)?.ExternalStatusName ?? string.Empty;

    private static bool MatchesIncidentSearch(IncidentReadDto incident, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var ticketId = GetRedmineTicketLink(incident)?.ExternalTicketId;
        return Contains(incident.Summary, searchText)
            || Contains(incident.ServerRoomName, searchText)
            || Contains(incident.ShellyDeviceName, searchText)
            || Contains(incident.MonitoredDeviceName, searchText)
            || Contains(ticketId, searchText);
    }

    private static TicketLinkReadDto? GetRedmineTicketLink(IncidentReadDto incident)
        => incident.TicketLinks.FirstOrDefault(x => x.ProviderName == "Redmine")
            ?? incident.TicketLinks.FirstOrDefault();

    private static DateTime GetIncidentDate(IncidentReadDto incident)
        => incident.LastOccurredAtUtc ?? incident.OpenedAtUtc;

    private static bool Contains(string? value, string searchText)
        => value?.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase) == true;

    private static bool Matches(TernaryFilter filter, bool value)
        => filter == TernaryFilter.All
            || (filter == TernaryFilter.Yes && value)
            || (filter == TernaryFilter.No && !value);

    private static int GetPriorityRank(string priority)
        => priority switch
        {
            "Low" => 1,
            "Normal" => 2,
            "High" => 3,
            "Urgent" => 4,
            "Immediate" => 5,
            _ => 0
        };

    private static int GetStatusRank(string status)
        => status switch
        {
            "New" => 1,
            "In Progress" => 2,
            "Feedback" => 3,
            "Resolved" => 4,
            "Closed" => 5,
            "Rejected" => 6,
            _ => 0
        };
}

public enum TernaryFilter
{
    All,
    Yes,
    No
}

public enum IncidentSortOption
{
    DateNewest,
    DateOldest,
    PriorityHighest,
    PriorityLowest,
    Status,
    ServerRoomAscending,
    ServerRoomDescending
}

public enum PingResultSortOption
{
    RecordedNewest,
    RecordedOldest,
    RoundtripFastest,
    RoundtripSlowest,
    FailureCountHighest,
    FailureCountLowest
}

public enum SensorReadingSortOption
{
    RecordedNewest,
    RecordedOldest,
    TemperatureHighest,
    TemperatureLowest,
    BatteryHighest,
    BatteryLowest,
    BrightnessHighest,
    BrightnessLowest
}
