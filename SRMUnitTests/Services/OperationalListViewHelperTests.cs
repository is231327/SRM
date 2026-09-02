using SRMApp.Services;
using SRMShared.DTOs.Incident;
using SRMShared.DTOs.MonitoredDevicePingResult;
using SRMShared.DTOs.SensorReading;
using SRMShared.Entities;

namespace SRMUnitTests.Services;

public class OperationalListViewHelperTests
{
    [Test]
    public void FilterAndSortIncidents_AppliesCombinedFiltersAndSearch()
    {
        var incidents = new[]
        {
            Incident("Temperature warning", "Berlin", IncidentType.TemperatureWarningThresholdExceeded,
                IncidentSeverity.Warning, "High", "New", "7", new DateTime(2026, 8, 1)),
            Incident("Temperature critical", "Berlin", IncidentType.TemperatureCriticalThresholdExceeded,
                IncidentSeverity.Critical, "Immediate", "In Progress", "8", new DateTime(2026, 8, 2)),
            Incident("Temperature warning", "Hamburg", IncidentType.TemperatureWarningThresholdExceeded,
                IncidentSeverity.Warning, "High", "New", "9", new DateTime(2026, 8, 3))
        };

        var result = OperationalListViewHelper.FilterAndSortIncidents(
            incidents,
            "Berlin",
            IncidentType.TemperatureWarningThresholdExceeded,
            "High",
            "New",
            IncidentSortOption.DateNewest);

        Assert.That(result.Select(x => x.TicketLinks[0].ExternalTicketId), Is.EqualTo(new[] { "7" }));
    }

    [Test]
    public void FilterAndSortIncidents_SortsByEverySupportedDimension()
    {
        var incidents = new[]
        {
            Incident("First", "Zurich", IncidentType.DoorOpenOutsideMaintenanceWindow,
                IncidentSeverity.Major, "Urgent", "Feedback", "1", new DateTime(2026, 8, 1)),
            Incident("Second", "Amsterdam", IncidentType.MonitoredDeviceFailureThresholdReached,
                IncidentSeverity.Critical, "Immediate", "New", "2", new DateTime(2026, 8, 3)),
            Incident("Third", "Berlin", IncidentType.TemperatureWarningThresholdExceeded,
                IncidentSeverity.Warning, "High", "Closed", "3", new DateTime(2026, 8, 2))
        };

        Assert.Multiple(() =>
        {
            Assert.That(SortedIncidentIds(incidents, IncidentSortOption.DateNewest), Is.EqualTo(new[] { "2", "3", "1" }));
            Assert.That(SortedIncidentIds(incidents, IncidentSortOption.DateOldest), Is.EqualTo(new[] { "1", "3", "2" }));
            Assert.That(SortedIncidentIds(incidents, IncidentSortOption.PriorityHighest), Is.EqualTo(new[] { "2", "1", "3" }));
            Assert.That(SortedIncidentIds(incidents, IncidentSortOption.PriorityLowest), Is.EqualTo(new[] { "3", "1", "2" }));
            Assert.That(SortedIncidentIds(incidents, IncidentSortOption.Status), Is.EqualTo(new[] { "2", "1", "3" }));
            Assert.That(SortedIncidentIds(incidents, IncidentSortOption.ServerRoomAscending), Is.EqualTo(new[] { "2", "3", "1" }));
            Assert.That(SortedIncidentIds(incidents, IncidentSortOption.ServerRoomDescending), Is.EqualTo(new[] { "1", "3", "2" }));
        });
    }

    [Test]
    public void IncidentColors_ShouldUseSynchronizedRedminePriority()
    {
        var incident = Incident(
            "Priority changed externally",
            "Berlin",
            IncidentType.TemperatureCriticalThresholdExceeded,
            IncidentSeverity.Critical,
            "Low",
            "New",
            "10",
            new DateTime(2026, 8, 4));

        Assert.Multiple(() =>
        {
            Assert.That(OverviewUiHelper.GetIncidentPriorityName(incident), Is.EqualTo("Low"));
            Assert.That(OverviewUiHelper.IsCriticalIncident(incident), Is.False);
            Assert.That(OverviewUiHelper.GetIncidentStateClass(incident), Is.EqualTo("alert-info"));
        });
    }

    [Test]
    public void FilterAndSortPingResults_AppliesFiltersAndSorts()
    {
        var oldest = Ping(false, true, 0, 3, new DateTime(2026, 8, 1));
        var middle = Ping(true, false, 80, 0, new DateTime(2026, 8, 2));
        var newest = Ping(true, true, 20, 5, new DateTime(2026, 8, 3));
        var results = new[] { oldest, middle, newest };

        var filtered = OperationalListViewHelper.FilterAndSortPingResults(
            results, TernaryFilter.Yes, TernaryFilter.Yes, PingResultSortOption.RecordedNewest);

        Assert.Multiple(() =>
        {
            Assert.That(filtered, Is.EqualTo(new[] { newest }));
            Assert.That(SortedPingResults(results, PingResultSortOption.RecordedOldest), Is.EqualTo(new[] { oldest, middle, newest }));
            Assert.That(SortedPingResults(results, PingResultSortOption.RoundtripFastest), Is.EqualTo(new[] { oldest, newest, middle }));
            Assert.That(SortedPingResults(results, PingResultSortOption.RoundtripSlowest), Is.EqualTo(new[] { middle, newest, oldest }));
            Assert.That(SortedPingResults(results, PingResultSortOption.FailureCountHighest), Is.EqualTo(new[] { newest, oldest, middle }));
            Assert.That(SortedPingResults(results, PingResultSortOption.FailureCountLowest), Is.EqualTo(new[] { middle, oldest, newest }));
        });
    }

    [Test]
    public void FilterAndSortSensorReadings_AppliesDoorFilterAndSorts()
    {
        var oldest = Reading(false, 18, 90, 100, new DateTime(2026, 8, 1));
        var middle = Reading(true, 25, 30, 900, new DateTime(2026, 8, 2));
        var newest = Reading(true, 21, 60, 500, new DateTime(2026, 8, 3));
        var readings = new[] { oldest, middle, newest };

        var filtered = OperationalListViewHelper.FilterAndSortSensorReadings(
            readings, TernaryFilter.Yes, SensorReadingSortOption.RecordedNewest);

        Assert.Multiple(() =>
        {
            Assert.That(filtered, Is.EqualTo(new[] { newest, middle }));
            Assert.That(SortedReadings(readings, SensorReadingSortOption.RecordedOldest), Is.EqualTo(new[] { oldest, middle, newest }));
            Assert.That(SortedReadings(readings, SensorReadingSortOption.TemperatureHighest), Is.EqualTo(new[] { middle, newest, oldest }));
            Assert.That(SortedReadings(readings, SensorReadingSortOption.TemperatureLowest), Is.EqualTo(new[] { oldest, newest, middle }));
            Assert.That(SortedReadings(readings, SensorReadingSortOption.BatteryHighest), Is.EqualTo(new[] { oldest, newest, middle }));
            Assert.That(SortedReadings(readings, SensorReadingSortOption.BatteryLowest), Is.EqualTo(new[] { middle, newest, oldest }));
            Assert.That(SortedReadings(readings, SensorReadingSortOption.BrightnessHighest), Is.EqualTo(new[] { middle, newest, oldest }));
            Assert.That(SortedReadings(readings, SensorReadingSortOption.BrightnessLowest), Is.EqualTo(new[] { oldest, newest, middle }));
        });
    }

    private static string[] SortedIncidentIds(IEnumerable<IncidentReadDto> incidents, IncidentSortOption sort)
        => OperationalListViewHelper.FilterAndSortIncidents(incidents, string.Empty, null, string.Empty, string.Empty, sort)
            .Select(x => x.TicketLinks[0].ExternalTicketId)
            .ToArray();

    private static MonitoredDevicePingResultReadDto[] SortedPingResults(
        IEnumerable<MonitoredDevicePingResultReadDto> results,
        PingResultSortOption sort)
        => OperationalListViewHelper.FilterAndSortPingResults(results, TernaryFilter.All, TernaryFilter.All, sort).ToArray();

    private static SensorReadingReadDto[] SortedReadings(
        IEnumerable<SensorReadingReadDto> readings,
        SensorReadingSortOption sort)
        => OperationalListViewHelper.FilterAndSortSensorReadings(readings, TernaryFilter.All, sort).ToArray();

    private static IncidentReadDto Incident(
        string summary,
        string serverRoom,
        IncidentType type,
        IncidentSeverity severity,
        string priority,
        string status,
        string ticketId,
        DateTime occurredAt)
        => new()
        {
            Summary = summary,
            ServerRoomName = serverRoom,
            Type = type,
            Severity = severity,
            OpenedAtUtc = occurredAt,
            LastOccurredAtUtc = occurredAt,
            TicketLinks =
            [
                new TicketLinkReadDto
                {
                    ProviderName = "Redmine",
                    ExternalTicketId = ticketId,
                    ExternalPriorityName = priority,
                    ExternalStatusName = status
                }
            ]
        };

    private static MonitoredDevicePingResultReadDto Ping(
        bool reachable,
        bool thresholdReached,
        long roundtrip,
        int failureCount,
        DateTime recordedAt)
        => new()
        {
            IsReachable = reachable,
            FailureThresholdReached = thresholdReached,
            RoundtripTimeMilliseconds = roundtrip,
            ConsecutiveFailureCount = failureCount,
            RecordedAtUtc = recordedAt
        };

    private static SensorReadingReadDto Reading(
        bool doorOpen,
        float temperature,
        float battery,
        float brightness,
        DateTime recordedAt)
        => new()
        {
            DoorOpen = doorOpen,
            TemperatureCelsius = temperature,
            BatteryPercent = battery,
            Brightness = brightness,
            RecordedAtUtc = recordedAt
        };
}
