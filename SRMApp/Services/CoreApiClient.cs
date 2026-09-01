using System.Net.Http.Json;
using System.Net.Http.Headers;
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

public class CoreApiClient(
    HttpClient httpClient,
    AuthSessionService authSessionService,
    IAuthApiClient authApiClient,
    ILogger<CoreApiClient> logger) : ICoreApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<List<CustomerReadDto>> GetCustomersAsync() => await GetListAsync<CustomerReadDto>("api/customers");
    public async Task<CustomerReadDto?> GetCustomerAsync(Guid id) => await GetAsync<CustomerReadDto>($"api/customers/{id}");
    public async Task<CustomerReadDto?> CreateCustomerAsync(CustomerCreateDto dto) => await PostAsync<CustomerCreateDto, CustomerReadDto>("api/customers", dto);
    public async Task<CustomerReadDto?> UpdateCustomerAsync(Guid id, CustomerUpdateDto dto) => await PutAsync<CustomerUpdateDto, CustomerReadDto>($"api/customers/{id}", dto);
    public async Task<bool> DeleteCustomerAsync(Guid id) => await DeleteAsync($"api/customers/{id}");

    public async Task<List<ServerRoomReadDto>> GetServerRoomsAsync() => await GetListAsync<ServerRoomReadDto>("api/serverrooms");
    public async Task<ServerRoomReadDto?> CreateServerRoomAsync(ServerRoomCreateDto dto) => await PostAsync<ServerRoomCreateDto, ServerRoomReadDto>("api/serverrooms", dto);
    public async Task<ServerRoomReadDto?> UpdateServerRoomAsync(Guid id, ServerRoomUpdateDto dto) => await PutAsync<ServerRoomUpdateDto, ServerRoomReadDto>($"api/serverrooms/{id}", dto);
    public async Task<bool> DeleteServerRoomAsync(Guid id) => await DeleteAsync($"api/serverrooms/{id}");

    public async Task<List<AgentReadDto>> GetAgentsAsync() => await GetListAsync<AgentReadDto>("api/agents");
    public async Task<AgentReadDto?> CreateAgentAsync(AgentCreateDto dto) => await PostAsync<AgentCreateDto, AgentReadDto>("api/agents", dto);
    public async Task<AgentReadDto?> UpdateAgentAsync(Guid id, AgentUpdateDto dto) => await PutAsync<AgentUpdateDto, AgentReadDto>($"api/agents/{id}", dto);
    public async Task<bool> DeleteAgentAsync(Guid id) => await DeleteAsync($"api/agents/{id}");

    public async Task<List<ShellyDeviceReadDto>> GetShellyDevicesAsync() => await GetListAsync<ShellyDeviceReadDto>("api/shellydevices");
    public async Task<ShellyDeviceReadDto?> CreateShellyDeviceAsync(ShellyDeviceCreateDto dto) => await PostAsync<ShellyDeviceCreateDto, ShellyDeviceReadDto>("api/shellydevices", dto);
    public async Task<ShellyDeviceReadDto?> UpdateShellyDeviceAsync(Guid id, ShellyDeviceUpdateDto dto) => await PutAsync<ShellyDeviceUpdateDto, ShellyDeviceReadDto>($"api/shellydevices/{id}", dto);
    public async Task<bool> DeleteShellyDeviceAsync(Guid id) => await DeleteAsync($"api/shellydevices/{id}");

    public async Task<List<MonitoredDeviceReadDto>> GetMonitoredDevicesAsync() => await GetListAsync<MonitoredDeviceReadDto>("api/monitoreddevices");
    public async Task<MonitoredDeviceReadDto?> CreateMonitoredDeviceAsync(MonitoredDeviceCreateDto dto) => await PostAsync<MonitoredDeviceCreateDto, MonitoredDeviceReadDto>("api/monitoreddevices", dto);
    public async Task<MonitoredDeviceReadDto?> UpdateMonitoredDeviceAsync(Guid id, MonitoredDeviceUpdateDto dto) => await PutAsync<MonitoredDeviceUpdateDto, MonitoredDeviceReadDto>($"api/monitoreddevices/{id}", dto);
    public async Task<bool> DeleteMonitoredDeviceAsync(Guid id) => await DeleteAsync($"api/monitoreddevices/{id}");

    public async Task<List<MonitoredDevicePingResultReadDto>> GetMonitoredDevicePingResultsAsync() => await GetListAsync<MonitoredDevicePingResultReadDto>("api/monitoreddevicepingresults");

    public async Task<List<MaintenanceWindowReadDto>> GetMaintenanceWindowsAsync() => await GetListAsync<MaintenanceWindowReadDto>("api/maintenancewindows");
    public async Task<MaintenanceWindowReadDto?> CreateMaintenanceWindowAsync(MaintenanceWindowCreateDto dto) => await PostAsync<MaintenanceWindowCreateDto, MaintenanceWindowReadDto>("api/maintenancewindows", dto);
    public async Task<MaintenanceWindowReadDto?> UpdateMaintenanceWindowAsync(Guid id, MaintenanceWindowUpdateDto dto) => await PutAsync<MaintenanceWindowUpdateDto, MaintenanceWindowReadDto>($"api/maintenancewindows/{id}", dto);
    public async Task<bool> DeleteMaintenanceWindowAsync(Guid id) => await DeleteAsync($"api/maintenancewindows/{id}");

    public async Task<List<SensorReadingReadDto>> GetSensorReadingsAsync() => await GetListAsync<SensorReadingReadDto>("api/sensorreadings");
    public async Task<List<IncidentReadDto>> GetIncidentsAsync(bool includeClosed = false)
        => await GetListAsync<IncidentReadDto>($"api/incidents?includeClosed={includeClosed.ToString().ToLowerInvariant()}");

    private async Task<List<T>> GetListAsync<T>(string path)
    {
        ConfigureBaseAddress();
        var ensured = await authApiClient.EnsureAccessTokenAsync();
        if (!ensured)
        {
            return [];
        }

        ApplyBearerToken();
        try
        {
            return await _httpClient.GetFromJsonAsync<List<T>>(path) ?? [];
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Core API list request to {Path} failed.", path);
            return [];
        }
    }

    private async Task<T?> GetAsync<T>(string path)
    {
        ConfigureBaseAddress();
        var ensured = await authApiClient.EnsureAccessTokenAsync();
        if (!ensured)
        {
            return default;
        }

        ApplyBearerToken();
        try
        {
            return await _httpClient.GetFromJsonAsync<T>(path);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Core API GET request to {Path} failed.", path);
            return default;
        }
    }

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest dto)
    {
        ConfigureBaseAddress();
        var ensured = await authApiClient.EnsureAccessTokenAsync();
        if (!ensured)
        {
            return default;
        }

        ApplyBearerToken();
        try
        {
            var response = await _httpClient.PostAsJsonAsync(path, dto);
            return await ReadResponseAsync<TResponse>(response);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Core API POST request to {Path} failed.", path);
            return default;
        }
    }

    private async Task<TResponse?> PutAsync<TRequest, TResponse>(string path, TRequest dto)
    {
        ConfigureBaseAddress();
        var ensured = await authApiClient.EnsureAccessTokenAsync();
        if (!ensured)
        {
            return default;
        }

        ApplyBearerToken();
        try
        {
            var response = await _httpClient.PutAsJsonAsync(path, dto);
            return await ReadResponseAsync<TResponse>(response);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Core API PUT request to {Path} failed.", path);
            return default;
        }
    }

    private async Task<bool> DeleteAsync(string path)
    {
        ConfigureBaseAddress();
        var ensured = await authApiClient.EnsureAccessTokenAsync();
        if (!ensured)
        {
            return false;
        }

        ApplyBearerToken();
        try
        {
            var response = await _httpClient.DeleteAsync(path);
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Core API DELETE request to {Path} failed.", path);
            return false;
        }
    }

    private async Task<TResponse?> ReadResponseAsync<TResponse>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    private void ConfigureBaseAddress()
    {
        ArgumentNullException.ThrowIfNull(_httpClient.BaseAddress);
    }

    private void ApplyBearerToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = authSessionService.IsAuthenticated
            ? new AuthenticationHeaderValue("Bearer", authSessionService.AccessToken)
            : null;
    }
}
