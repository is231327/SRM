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

public interface ICrudPageDataService
{
    Task<AgentPageData> GetAgentPageDataAsync(Guid? serverRoomId, Guid? customerId);
    Task<ServerRoomPageData> GetServerRoomPageDataAsync(Guid? customerId);
    Task<ShellyDevicePageData> GetShellyDevicePageDataAsync(Guid? agentId, Guid? serverRoomId, Guid? customerId);
    Task<MonitoredDevicePageData> GetMonitoredDevicePageDataAsync(Guid? agentId, Guid? serverRoomId, Guid? customerId);
    Task<MaintenanceWindowPageData> GetMaintenanceWindowPageDataAsync(Guid? serverRoomId, Guid? customerId);
    Task<SensorReadingPageData> GetSensorReadingPageDataAsync(Guid? shellyDeviceId);
    Task<MonitoredDevicePingResultPageData> GetMonitoredDevicePingResultPageDataAsync(Guid? monitoredDeviceId);
}

public sealed class CrudPageDataService(ICoreApiClient apiClient) : ICrudPageDataService
{
    public async Task<AgentPageData> GetAgentPageDataAsync(Guid? serverRoomId, Guid? customerId)
    {
        var agents = await apiClient.GetAgentsAsync();
        var serverRooms = await apiClient.GetServerRoomsAsync();

        if (customerId is not null)
        {
            var customerRoomIds = serverRooms.Where(x => x.CustomerId == customerId.Value).Select(x => x.Id).ToHashSet();
            return new AgentPageData(
                agents.Where(x => customerRoomIds.Contains(x.ServerRoomId)).ToList(),
                serverRooms.Where(x => customerRoomIds.Contains(x.Id)).ToList());
        }

        return new AgentPageData(
            serverRoomId is null ? agents : agents.Where(x => x.ServerRoomId == serverRoomId).ToList(),
            serverRooms);
    }

    public async Task<ServerRoomPageData> GetServerRoomPageDataAsync(Guid? customerId)
    {
        var serverRooms = await apiClient.GetServerRoomsAsync();
        var customers = await apiClient.GetCustomersAsync();

        return new ServerRoomPageData(
            customerId is null ? serverRooms : serverRooms.Where(x => x.CustomerId == customerId).ToList(),
            customers);
    }

    public async Task<ShellyDevicePageData> GetShellyDevicePageDataAsync(Guid? agentId, Guid? serverRoomId, Guid? customerId)
    {
        var shellyDevices = await apiClient.GetShellyDevicesAsync();
        var agents = await apiClient.GetAgentsAsync();

        if (customerId is not null || serverRoomId is not null)
        {
            HashSet<Guid> roomIds = serverRoomId is not null
                ? [serverRoomId.Value]
                : (await apiClient.GetServerRoomsAsync()).Where(x => x.CustomerId == customerId!.Value).Select(x => x.Id).ToHashSet();
            var customerAgentIds = agents.Where(x => roomIds.Contains(x.ServerRoomId)).Select(x => x.Id).ToHashSet();

            return new ShellyDevicePageData(
                shellyDevices.Where(x => customerAgentIds.Contains(x.AgentId)).ToList(),
                agents.Where(x => customerAgentIds.Contains(x.Id)).ToList());
        }

        return new ShellyDevicePageData(
            agentId is null ? shellyDevices : shellyDevices.Where(x => x.AgentId == agentId).ToList(),
            agents);
    }

    public async Task<MonitoredDevicePageData> GetMonitoredDevicePageDataAsync(Guid? agentId, Guid? serverRoomId, Guid? customerId)
    {
        var monitoredDevices = await apiClient.GetMonitoredDevicesAsync();
        var agents = await apiClient.GetAgentsAsync();
        var pingResults = await apiClient.GetMonitoredDevicePingResultsAsync();
        var incidents = await apiClient.GetIncidentsAsync();

        if (customerId is not null || serverRoomId is not null)
        {
            HashSet<Guid> customerRoomIds = serverRoomId is not null
                ? [serverRoomId.Value]
                : (await apiClient.GetServerRoomsAsync()).Where(x => x.CustomerId == customerId!.Value).Select(x => x.Id).ToHashSet();
            var customerAgentIds = agents.Where(x => customerRoomIds.Contains(x.ServerRoomId)).Select(x => x.Id).ToHashSet();
            monitoredDevices = monitoredDevices.Where(x => customerAgentIds.Contains(x.AgentId)).ToList();
            agents = agents.Where(x => customerAgentIds.Contains(x.Id)).ToList();
        }
        else if (agentId is not null)
        {
            monitoredDevices = monitoredDevices.Where(x => x.AgentId == agentId).ToList();
        }

        var monitoredDeviceIds = monitoredDevices.Select(x => x.Id).ToHashSet();
        var latestPingByDeviceId = pingResults
            .Where(x => monitoredDeviceIds.Contains(x.MonitoredDeviceId))
            .GroupBy(x => x.MonitoredDeviceId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.RecordedAtUtc).First());
        var openCriticalIncidentDeviceIds = incidents
            .Where(x => x.MonitoredDeviceId.HasValue && OverviewUiHelper.IsOpenIncident(x) && x.Severity.ToString() == "Critical")
            .Select(x => x.MonitoredDeviceId!.Value)
            .ToHashSet();

        return new MonitoredDevicePageData(
            monitoredDevices,
            agents,
            latestPingByDeviceId,
            openCriticalIncidentDeviceIds);
    }

    public async Task<MaintenanceWindowPageData> GetMaintenanceWindowPageDataAsync(Guid? serverRoomId, Guid? customerId)
    {
        var windows = await apiClient.GetMaintenanceWindowsAsync();
        var serverRooms = await apiClient.GetServerRoomsAsync();

        if (customerId is not null)
        {
            var customerRoomIds = serverRooms.Where(x => x.CustomerId == customerId.Value).Select(x => x.Id).ToHashSet();
            return new MaintenanceWindowPageData(
                windows.Where(x => customerRoomIds.Contains(x.ServerRoomId)).ToList(),
                serverRooms.Where(x => customerRoomIds.Contains(x.Id)).ToList());
        }

        return new MaintenanceWindowPageData(
            serverRoomId is null ? windows : windows.Where(x => x.ServerRoomId == serverRoomId).ToList(),
            serverRooms);
    }

    public async Task<SensorReadingPageData> GetSensorReadingPageDataAsync(Guid? shellyDeviceId)
    {
        var sensorReadings = await apiClient.GetSensorReadingsAsync();

        return new SensorReadingPageData(
            shellyDeviceId is null ? sensorReadings : sensorReadings.Where(x => x.ShellyDeviceId == shellyDeviceId).ToList());
    }

    public async Task<MonitoredDevicePingResultPageData> GetMonitoredDevicePingResultPageDataAsync(Guid? monitoredDeviceId)
    {
        var pingResults = await apiClient.GetMonitoredDevicePingResultsAsync();

        return new MonitoredDevicePingResultPageData(
            monitoredDeviceId is null ? pingResults : pingResults.Where(x => x.MonitoredDeviceId == monitoredDeviceId).ToList());
    }
}

public sealed record AgentPageData(
    IReadOnlyList<AgentReadDto> Agents,
    IReadOnlyList<ServerRoomReadDto> ServerRoomsForSelection);

public sealed record ServerRoomPageData(
    IReadOnlyList<ServerRoomReadDto> ServerRooms,
    IReadOnlyList<CustomerReadDto> CustomersForSelection);

public sealed record ShellyDevicePageData(
    IReadOnlyList<ShellyDeviceReadDto> Items,
    IReadOnlyList<AgentReadDto> AgentsForSelection);

public sealed record MonitoredDevicePageData(
    IReadOnlyList<MonitoredDeviceReadDto> Items,
    IReadOnlyList<AgentReadDto> AgentsForSelection,
    IReadOnlyDictionary<Guid, MonitoredDevicePingResultReadDto> LatestPingByDeviceId,
    IReadOnlySet<Guid> OpenCriticalIncidentDeviceIds);

public sealed record MaintenanceWindowPageData(
    IReadOnlyList<MaintenanceWindowReadDto> Items,
    IReadOnlyList<ServerRoomReadDto> ServerRoomsForSelection);

public sealed record SensorReadingPageData(
    IReadOnlyList<SensorReadingReadDto> Items);

public sealed record MonitoredDevicePingResultPageData(
    IReadOnlyList<MonitoredDevicePingResultReadDto> Items);
