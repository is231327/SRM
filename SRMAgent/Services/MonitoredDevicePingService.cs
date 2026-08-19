using System.Net.NetworkInformation;
using SRMAgent.Models.Monitoring;
using SRMAgent.Services.Interfaces;
using SRMShared.DTOs.MonitoredDevice;

namespace SRMAgent.Services;

public class MonitoredDevicePingService : IMonitoredDevicePingService
{
    public async Task<MonitoredDevicePingResult> PingAsync(MonitoredDeviceReadDto device, CancellationToken cancellationToken = default)
    {
        using var ping = new Ping();

        try
        {
            var reply = await ping.SendPingAsync(device.IpAddress, device.TimeoutMilliseconds);
            return new MonitoredDevicePingResult
            {
                MonitoredDeviceId = device.Id,
                DisplayName = device.DisplayName,
                IpAddress = device.IpAddress,
                IsReachable = reply.Status == IPStatus.Success,
                RoundtripTimeMilliseconds = reply.Status == IPStatus.Success ? reply.RoundtripTime : 0,
                ErrorMessage = reply.Status == IPStatus.Success ? string.Empty : reply.Status.ToString()
            };
        }
        catch (Exception ex) when (ex is PingException or InvalidOperationException or ArgumentException)
        {
            return new MonitoredDevicePingResult
            {
                MonitoredDeviceId = device.Id,
                DisplayName = device.DisplayName,
                IpAddress = device.IpAddress,
                IsReachable = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
