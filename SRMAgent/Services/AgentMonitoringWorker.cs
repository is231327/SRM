using Microsoft.Extensions.Options;
using SRMAgent.Configuration;
using SRMAgent.Models.Monitoring;
using SRMAgent.Services.Interfaces;
using SRMShared.DTOs.AgentReporting;
using SRMShared.DTOs.AgentRuntime;

namespace SRMAgent.Services;

public class AgentMonitoringWorker(
    IServiceScopeFactory serviceScopeFactory,
    AgentRuntimeCache runtimeCache,
    IOptions<AgentRuntimeOptions> runtimeOptions,
    ILogger<AgentMonitoringWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingDelay = TimeSpan.FromSeconds(Math.Max(5, runtimeOptions.Value.PollingIntervalSeconds));
        var configurationRefreshIntervalSeconds = Math.Max(30, runtimeOptions.Value.ConfigurationRefreshIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<AgentMonitoringOrchestrator>();
                var refreshConfiguration = runtimeCache.ShouldRefreshConfiguration(configurationRefreshIntervalSeconds);
                var result = await orchestrator.ExecuteCycleAsync(refreshConfiguration, stoppingToken);

                if (refreshConfiguration)
                {
                    runtimeCache.Update(result.Configuration);
                }
                runtimeCache.MarkCycleExecuted();
                logger.LogInformation(
                    "Agent monitoring cycle finished. Submitted readings: {SensorReadingCount}. Ping checks: {PingCount}.",
                    result.Result.SubmittedSensorReadings.Count,
                    result.Result.PingResults.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Agent monitoring cycle failed.");
            }

            try
            {
                await Task.Delay(pollingDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
