using SRMShared.DTOs.Agent;
using SRMShared.DTOs.Customer;
using SRMShared.DTOs.MaintenanceWindow;
using SRMShared.DTOs.MonitoredDevice;
using SRMShared.DTOs.MonitoredDevicePingResult;
using SRMShared.DTOs.SensorReading;
using SRMShared.DTOs.ServerRoom;
using SRMShared.DTOs.ShellyDevice;

namespace SRMApp.Services;

public interface ICoreApiClient
{
    Task<List<CustomerReadDto>> GetCustomersAsync();
    Task<CustomerReadDto?> GetCustomerAsync(Guid id);
    Task<CustomerReadDto?> CreateCustomerAsync(CustomerCreateDto dto);
    Task<CustomerReadDto?> UpdateCustomerAsync(Guid id, CustomerUpdateDto dto);
    Task<bool> DeleteCustomerAsync(Guid id);

    Task<List<ServerRoomReadDto>> GetServerRoomsAsync();
    Task<ServerRoomReadDto?> CreateServerRoomAsync(ServerRoomCreateDto dto);
    Task<ServerRoomReadDto?> UpdateServerRoomAsync(Guid id, ServerRoomUpdateDto dto);
    Task<bool> DeleteServerRoomAsync(Guid id);

    Task<List<AgentReadDto>> GetAgentsAsync();
    Task<AgentReadDto?> CreateAgentAsync(AgentCreateDto dto);
    Task<AgentReadDto?> UpdateAgentAsync(Guid id, AgentUpdateDto dto);
    Task<bool> DeleteAgentAsync(Guid id);

    Task<List<ShellyDeviceReadDto>> GetShellyDevicesAsync();
    Task<ShellyDeviceReadDto?> CreateShellyDeviceAsync(ShellyDeviceCreateDto dto);
    Task<ShellyDeviceReadDto?> UpdateShellyDeviceAsync(Guid id, ShellyDeviceUpdateDto dto);
    Task<bool> DeleteShellyDeviceAsync(Guid id);

    Task<List<MonitoredDeviceReadDto>> GetMonitoredDevicesAsync();
    Task<MonitoredDeviceReadDto?> CreateMonitoredDeviceAsync(MonitoredDeviceCreateDto dto);
    Task<MonitoredDeviceReadDto?> UpdateMonitoredDeviceAsync(Guid id, MonitoredDeviceUpdateDto dto);
    Task<bool> DeleteMonitoredDeviceAsync(Guid id);

    Task<List<MonitoredDevicePingResultReadDto>> GetMonitoredDevicePingResultsAsync();
    Task<MonitoredDevicePingResultReadDto?> CreateMonitoredDevicePingResultAsync(MonitoredDevicePingResultCreateDto dto);
    Task<MonitoredDevicePingResultReadDto?> UpdateMonitoredDevicePingResultAsync(Guid id, MonitoredDevicePingResultUpdateDto dto);
    Task<bool> DeleteMonitoredDevicePingResultAsync(Guid id);

    Task<List<MaintenanceWindowReadDto>> GetMaintenanceWindowsAsync();
    Task<MaintenanceWindowReadDto?> CreateMaintenanceWindowAsync(MaintenanceWindowCreateDto dto);
    Task<MaintenanceWindowReadDto?> UpdateMaintenanceWindowAsync(Guid id, MaintenanceWindowUpdateDto dto);
    Task<bool> DeleteMaintenanceWindowAsync(Guid id);

    Task<List<SensorReadingReadDto>> GetSensorReadingsAsync();
    Task<SensorReadingReadDto?> CreateSensorReadingAsync(SensorReadingCreateDto dto);
    Task<SensorReadingReadDto?> UpdateSensorReadingAsync(Guid id, SensorReadingUpdateDto dto);
    Task<bool> DeleteSensorReadingAsync(Guid id);
}
