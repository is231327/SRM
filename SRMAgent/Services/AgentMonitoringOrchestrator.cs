using SRMAgent.Models.Monitoring;
using SRMAgent.Services.Interfaces;
using SRMShared.DTOs.AgentRuntime;
using SRMShared.DTOs.AgentReporting;

namespace SRMAgent.Services;

public class AgentMonitoringOrchestrator(
    IAgentAuthApiClient authApiClient,
    IAgentRuntimeApiClient runtimeApiClient,
    IAgentCoreApiClient coreApiClient,
    IVirtualShellyClient virtualShellyClient,
    IMonitoredDevicePingService monitoredDevicePingService,
    AgentRuntimeCache runtimeCache,
    IRetryExecutor retryExecutor,
    ILogger<AgentMonitoringOrchestrator> logger)
{
    public async Task ProcessWebhookAsync(Guid shellyDeviceId, SRMAgent.Models.Shelly.VirtualShellyStatusResponse payload, CancellationToken cancellationToken = default)
    {
        var accessToken = await retryExecutor.ExecuteAsync(
            authApiClient.LoginAsync,
            maxAttempts: 3,
            initialDelay: TimeSpan.FromSeconds(1),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Agent login failed.");
        }

        await retryExecutor.ExecuteAsync(
            ct => coreApiClient.SubmitSensorReadingAsync(
                accessToken,
                new AgentSensorReadingReportDto
                {
                    ShellyDeviceId = shellyDeviceId,
                    TemperatureCelsius = payload.Temperature?.Celsius ?? payload.Temperature?.Value ?? 0,
                    BatteryPercent = payload.Battery?.Value ?? 0,
                    Brightness = payload.Lux?.Value ?? 0,
                    DoorOpen = string.Equals(payload.Sensor?.State, "open", StringComparison.OrdinalIgnoreCase),
                    RecordedAtUtc = DateTime.UtcNow
                },
                ct),
            maxAttempts: 3,
            initialDelay: TimeSpan.FromSeconds(1),
            cancellationToken);
    }

    public async Task<AgentMonitoringExecution> ExecuteCycleAsync(bool refreshConfiguration, CancellationToken cancellationToken = default)
    {
        var accessToken = await retryExecutor.ExecuteAsync(
            authApiClient.LoginAsync,
            maxAttempts: 3,
            initialDelay: TimeSpan.FromSeconds(1),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Agent login failed.");
        }

        var configuration = refreshConfiguration || runtimeCache.CurrentConfiguration is null
            ? await retryExecutor.ExecuteAsync(
                ct => runtimeApiClient.GetConfigurationAsync(accessToken, ct),
                maxAttempts: 3,
                initialDelay: TimeSpan.FromSeconds(1),
                cancellationToken)
            : runtimeCache.CurrentConfiguration;

        if (configuration is null)
        {
            throw new InvalidOperationException("No runtime configuration was returned for the authenticated agent.");
        }

        var cycleResult = new AgentMonitoringCycleResult();

        foreach (var shellyDevice in configuration.ShellyDevices.Where(x => x.IsActive))
        {
            var shellyStatus = await retryExecutor.ExecuteAsync(
                ct => virtualShellyClient.GetStatusAsync(shellyDevice.BaseUrl, ct),
                maxAttempts: 3,
                initialDelay: TimeSpan.FromMilliseconds(500),
                cancellationToken);

            if (shellyStatus is null)
            {
                logger.LogWarning("Shelly device '{ShellyName}' at '{BaseUrl}' returned no status payload.", shellyDevice.Name, shellyDevice.BaseUrl);
                continue;
            }

            var submittedReading = await retryExecutor.ExecuteAsync(
                ct => coreApiClient.SubmitSensorReadingAsync(
                    accessToken,
                    new AgentSensorReadingReportDto
                    {
                        ShellyDeviceId = shellyDevice.Id,
                        TemperatureCelsius = shellyStatus.Temperature?.Celsius ?? shellyStatus.Temperature?.Value ?? 0,
                        BatteryPercent = shellyStatus.Battery?.Value ?? 0,
                        Brightness = shellyStatus.Lux?.Value ?? 0,
                        DoorOpen = string.Equals(shellyStatus.Sensor?.State, "open", StringComparison.OrdinalIgnoreCase),
                        RecordedAtUtc = DateTime.UtcNow
                    },
                    ct),
                maxAttempts: 3,
                initialDelay: TimeSpan.FromSeconds(1),
                cancellationToken);

            if (submittedReading is not null)
            {
                cycleResult.SubmittedSensorReadings.Add(submittedReading);
            }
        }

        foreach (var monitoredDevice in configuration.MonitoredDevices.Where(x => x.IsActive))
        {
            var pingResult = await monitoredDevicePingService.PingAsync(monitoredDevice, cancellationToken);
            pingResult.ConsecutiveFailureCount = runtimeCache.RegisterPingOutcome(monitoredDevice.Id, pingResult.IsReachable);
            pingResult.FailureThresholdReached = !pingResult.IsReachable && pingResult.ConsecutiveFailureCount >= monitoredDevice.FailureThreshold;
            cycleResult.PingResults.Add(pingResult);

            await retryExecutor.ExecuteAsync(
                ct => coreApiClient.SubmitPingResultAsync(
                    accessToken,
                    new AgentPingResultReportDto
                    {
                        MonitoredDeviceId = monitoredDevice.Id,
                        IsReachable = pingResult.IsReachable,
                        RoundtripTimeMilliseconds = pingResult.RoundtripTimeMilliseconds,
                        ConsecutiveFailureCount = pingResult.ConsecutiveFailureCount,
                        FailureThresholdReached = pingResult.FailureThresholdReached,
                        ErrorMessage = pingResult.ErrorMessage,
                        RecordedAtUtc = DateTime.UtcNow
                    },
                    ct),
                maxAttempts: 3,
                initialDelay: TimeSpan.FromSeconds(1),
                cancellationToken);
        }

        return new AgentMonitoringExecution
        {
            Configuration = configuration,
            Result = cycleResult
        };
    }
}
