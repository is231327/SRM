using SRMShared.DTOs.Agent;
using SRMShared.DTOs.MonitoredDevice;
using SRMShared.DTOs.ShellyDevice;

namespace SRMShared.DTOs.AgentRuntime;

public class AgentRuntimeConfigurationDto
{
    public AgentReadDto Agent { get; set; } = new();
    public IReadOnlyCollection<ShellyDeviceReadDto> ShellyDevices { get; set; } = Array.Empty<ShellyDeviceReadDto>();
    public IReadOnlyCollection<MonitoredDeviceReadDto> MonitoredDevices { get; set; } = Array.Empty<MonitoredDeviceReadDto>();
}
