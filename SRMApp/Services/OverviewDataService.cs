using SRMShared.DTOs.Agent;
using SRMShared.DTOs.Customer;
using SRMShared.DTOs.Incident;
using SRMShared.DTOs.MaintenanceWindow;
using SRMShared.DTOs.MonitoredDevice;
using SRMShared.DTOs.MonitoredDevicePingResult;
using SRMShared.DTOs.SensorReading;
using SRMShared.DTOs.ServerRoom;
using SRMShared.DTOs.ShellyDevice;

namespace SRMApp.Services;

public interface IOverviewDataService
{
    Task<DashboardOverviewData> GetDashboardOverviewAsync();
    Task<CustomerOverviewData?> GetCustomerOverviewAsync(Guid customerId);
    Task<ServerRoomOverviewData> GetServerRoomOverviewAsync(Guid serverRoomId, string fallbackServerRoomName);
}

public sealed class OverviewDataService(ICoreApiClient apiClient) : IOverviewDataService
{
    public async Task<DashboardOverviewData> GetDashboardOverviewAsync()
    {
        var customers = await apiClient.GetCustomersAsync();
        var serverRooms = await apiClient.GetServerRoomsAsync();
        var agents = await apiClient.GetAgentsAsync();
        var shellyDevices = await apiClient.GetShellyDevicesAsync();
        var monitoredDevices = await apiClient.GetMonitoredDevicesAsync();
        var maintenanceWindows = await apiClient.GetMaintenanceWindowsAsync();
        var sensorReadings = await apiClient.GetSensorReadingsAsync();
        var pingResults = await apiClient.GetMonitoredDevicePingResultsAsync();
        var incidents = await apiClient.GetIncidentsAsync();
        var openIncidents = incidents.Where(x => x.Status.ToString() == "Open").ToList();
        var criticalIncidents = incidents.Where(x => x.Severity.ToString() == "Critical").ToList();
        var roomIdsByCustomerId = serverRooms
            .GroupBy(x => x.CustomerId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToHashSet());
        var incidentCountByCustomerId = customers.ToDictionary(
            x => x.Id,
            x => incidents.Count(y => roomIdsByCustomerId.GetValueOrDefault(x.Id)?.Contains(y.ServerRoomId) == true));
        var lastActivityByCustomerId = customers.ToDictionary(
            x => x.Id,
            x =>
            {
                var value = incidents
                    .Where(y => roomIdsByCustomerId.GetValueOrDefault(x.Id)?.Contains(y.ServerRoomId) == true)
                    .Select(y => y.LastOccurredAtUtc ?? y.OpenedAtUtc)
                    .OrderByDescending(y => y)
                    .FirstOrDefault();

                return value == default ? (DateTime?)null : value;
            });
        var cardClassByCustomerId = customers.ToDictionary(
            x => x.Id,
            x =>
            {
                var customerIncidents = incidents
                    .Where(y => roomIdsByCustomerId.GetValueOrDefault(x.Id)?.Contains(y.ServerRoomId) == true)
                    .ToList();

                if (customerIncidents.Any(y => y.Severity.ToString() == "Critical" && y.Status.ToString() == "Open"))
                {
                    return "alert-critical";
                }

                if (customerIncidents.Any(y => y.Status.ToString() == "Open"))
                {
                    return "alert-warning";
                }

                return "alert-ok";
            });

        return new DashboardOverviewData(
            customers,
            serverRooms,
            agents,
            shellyDevices,
            monitoredDevices,
            maintenanceWindows,
            sensorReadings,
            pingResults,
            incidents,
            openIncidents,
            criticalIncidents,
            incidentCountByCustomerId,
            lastActivityByCustomerId,
            cardClassByCustomerId);
    }

    public async Task<CustomerOverviewData?> GetCustomerOverviewAsync(Guid customerId)
    {
        var customers = await apiClient.GetCustomersAsync();
        var customer = customers.FirstOrDefault(x => x.Id == customerId);
        if (customer is null)
        {
            return null;
        }

        var allServerRooms = await apiClient.GetServerRoomsAsync();
        var serverRooms = allServerRooms.Where(x => x.CustomerId == customerId).ToList();
        var roomIds = serverRooms.Select(x => x.Id).ToHashSet();

        var allAgents = await apiClient.GetAgentsAsync();
        var agents = allAgents.Where(x => roomIds.Contains(x.ServerRoomId)).ToList();
        var agentIds = agents.Select(x => x.Id).ToHashSet();

        var allShellyDevices = await apiClient.GetShellyDevicesAsync();
        var shellyDevices = allShellyDevices.Where(x => agentIds.Contains(x.AgentId)).ToList();
        var shellyIds = shellyDevices.Select(x => x.Id).ToHashSet();

        var allMonitoredDevices = await apiClient.GetMonitoredDevicesAsync();
        var monitoredDevices = allMonitoredDevices.Where(x => agentIds.Contains(x.AgentId)).ToList();
        var monitoredDeviceIds = monitoredDevices.Select(x => x.Id).ToHashSet();

        var allMaintenanceWindows = await apiClient.GetMaintenanceWindowsAsync();
        var maintenanceWindows = allMaintenanceWindows.Where(x => roomIds.Contains(x.ServerRoomId)).ToList();

        var allIncidents = await apiClient.GetIncidentsAsync();
        var incidents = allIncidents.Where(x => roomIds.Contains(x.ServerRoomId)).ToList();

        var allSensorReadings = await apiClient.GetSensorReadingsAsync();
        var sensorReadings = allSensorReadings.Where(x => shellyIds.Contains(x.ShellyDeviceId)).ToList();

        var allPingResults = await apiClient.GetMonitoredDevicePingResultsAsync();
        var pingResults = allPingResults.Where(x => monitoredDeviceIds.Contains(x.MonitoredDeviceId)).ToList();

        return new CustomerOverviewData(
            customer,
            serverRooms,
            agents,
            shellyDevices,
            monitoredDevices,
            maintenanceWindows,
            incidents,
            sensorReadings,
            pingResults,
            agents.GroupBy(x => x.ServerRoomId).ToDictionary(x => x.Key, x => (IReadOnlyList<AgentReadDto>)x.ToList()),
            shellyDevices.GroupBy(x => x.AgentId).ToDictionary(x => x.Key, x => (IReadOnlyList<ShellyDeviceReadDto>)x.ToList()),
            monitoredDevices.GroupBy(x => x.AgentId).ToDictionary(x => x.Key, x => (IReadOnlyList<MonitoredDeviceReadDto>)x.ToList()),
            sensorReadings.GroupBy(x => x.ShellyDeviceId).ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.RecordedAtUtc).First()),
            pingResults.GroupBy(x => x.MonitoredDeviceId).ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.RecordedAtUtc).First()));
    }

    public async Task<ServerRoomOverviewData> GetServerRoomOverviewAsync(Guid serverRoomId, string fallbackServerRoomName)
    {
        var serverRooms = await apiClient.GetServerRoomsAsync();
        var serverRoomName = serverRooms.FirstOrDefault(x => x.Id == serverRoomId)?.Name ?? fallbackServerRoomName;

        var agents = (await apiClient.GetAgentsAsync()).Where(x => x.ServerRoomId == serverRoomId).ToList();
        var agentIds = agents.Select(x => x.Id).ToHashSet();

        var shellyDevices = (await apiClient.GetShellyDevicesAsync()).Where(x => agentIds.Contains(x.AgentId)).ToList();
        var monitoredDevices = (await apiClient.GetMonitoredDevicesAsync()).Where(x => agentIds.Contains(x.AgentId)).ToList();
        var maintenanceWindows = (await apiClient.GetMaintenanceWindowsAsync()).Where(x => x.ServerRoomId == serverRoomId).ToList();
        var incidents = (await apiClient.GetIncidentsAsync()).Where(x => x.ServerRoomId == serverRoomId).ToList();

        var shellyIds = shellyDevices.Select(x => x.Id).ToHashSet();
        var monitoredDeviceIds = monitoredDevices.Select(x => x.Id).ToHashSet();

        var sensorReadings = (await apiClient.GetSensorReadingsAsync()).Where(x => shellyIds.Contains(x.ShellyDeviceId)).ToList();
        var pingResults = (await apiClient.GetMonitoredDevicePingResultsAsync()).Where(x => monitoredDeviceIds.Contains(x.MonitoredDeviceId)).ToList();

        var lastAgentSeenAtUtc = agents.Where(x => x.LastSeenAtUtc.HasValue).Select(x => x.LastSeenAtUtc).DefaultIfEmpty().Max();
        DateTime? lastSensorReadingAtUtc = sensorReadings.Count > 0 ? sensorReadings.Max(x => x.RecordedAtUtc) : null;
        DateTime? lastPingAtUtc = pingResults.Count > 0 ? pingResults.Max(x => x.RecordedAtUtc) : null;
        DateTime? lastIncidentAtUtc = incidents.Count > 0 ? incidents.Max(x => x.LastOccurredAtUtc ?? x.OpenedAtUtc) : null;
        var latestSensorReading = sensorReadings.OrderByDescending(x => x.RecordedAtUtc).FirstOrDefault();

        return new ServerRoomOverviewData(
            serverRoomName,
            agents,
            shellyDevices,
            monitoredDevices,
            maintenanceWindows,
            incidents,
            sensorReadings,
            pingResults,
            lastAgentSeenAtUtc,
            lastSensorReadingAtUtc,
            lastPingAtUtc,
            lastIncidentAtUtc,
            latestSensorReading);
    }
}

public sealed record CustomerOverviewData(
    CustomerReadDto Customer,
    IReadOnlyList<ServerRoomReadDto> ServerRooms,
    IReadOnlyList<AgentReadDto> Agents,
    IReadOnlyList<ShellyDeviceReadDto> ShellyDevices,
    IReadOnlyList<MonitoredDeviceReadDto> MonitoredDevices,
    IReadOnlyList<MaintenanceWindowReadDto> MaintenanceWindows,
    IReadOnlyList<IncidentReadDto> Incidents,
    IReadOnlyList<SensorReadingReadDto> SensorReadings,
    IReadOnlyList<MonitoredDevicePingResultReadDto> PingResults,
    IReadOnlyDictionary<Guid, IReadOnlyList<AgentReadDto>> AgentsByRoomId,
    IReadOnlyDictionary<Guid, IReadOnlyList<ShellyDeviceReadDto>> ShellyDevicesByAgentId,
    IReadOnlyDictionary<Guid, IReadOnlyList<MonitoredDeviceReadDto>> MonitoredDevicesByAgentId,
    IReadOnlyDictionary<Guid, SensorReadingReadDto> LatestSensorReadingByShellyId,
    IReadOnlyDictionary<Guid, MonitoredDevicePingResultReadDto> LatestPingResultByMonitoredDeviceId);

public sealed record DashboardOverviewData(
    IReadOnlyList<CustomerReadDto> Customers,
    IReadOnlyList<ServerRoomReadDto> ServerRooms,
    IReadOnlyList<AgentReadDto> Agents,
    IReadOnlyList<ShellyDeviceReadDto> ShellyDevices,
    IReadOnlyList<MonitoredDeviceReadDto> MonitoredDevices,
    IReadOnlyList<MaintenanceWindowReadDto> MaintenanceWindows,
    IReadOnlyList<SensorReadingReadDto> SensorReadings,
    IReadOnlyList<MonitoredDevicePingResultReadDto> PingResults,
    IReadOnlyList<IncidentReadDto> Incidents,
    IReadOnlyList<IncidentReadDto> OpenIncidents,
    IReadOnlyList<IncidentReadDto> CriticalIncidents,
    IReadOnlyDictionary<Guid, int> IncidentCountByCustomerId,
    IReadOnlyDictionary<Guid, DateTime?> LastActivityByCustomerId,
    IReadOnlyDictionary<Guid, string> CardClassByCustomerId);

public sealed record ServerRoomOverviewData(
    string ServerRoomName,
    IReadOnlyList<AgentReadDto> Agents,
    IReadOnlyList<ShellyDeviceReadDto> ShellyDevices,
    IReadOnlyList<MonitoredDeviceReadDto> MonitoredDevices,
    IReadOnlyList<MaintenanceWindowReadDto> MaintenanceWindows,
    IReadOnlyList<IncidentReadDto> Incidents,
    IReadOnlyList<SensorReadingReadDto> SensorReadings,
    IReadOnlyList<MonitoredDevicePingResultReadDto> PingResults,
    DateTime? LastAgentSeenAtUtc,
    DateTime? LastSensorReadingAtUtc,
    DateTime? LastPingAtUtc,
    DateTime? LastIncidentAtUtc,
    SensorReadingReadDto? LatestSensorReading);
