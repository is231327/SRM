using SRMAgent.Models.Monitoring;
using SRMShared.DTOs.MonitoredDevice;

namespace SRMAgent.Services.Interfaces;

public interface IMonitoredDevicePingService
{
    Task<MonitoredDevicePingResult> PingAsync(MonitoredDeviceReadDto device, CancellationToken cancellationToken = default);
}
