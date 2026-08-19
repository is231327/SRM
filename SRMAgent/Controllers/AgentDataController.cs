using Microsoft.AspNetCore.Mvc;
using SRMAgent.Models.Shelly;
using SRMAgent.Models.Monitoring;
using SRMAgent.Services;

namespace SRMAgent.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentDataController : ControllerBase
{
    private readonly AgentRuntimeCache _agentRuntimeCache;
    private readonly AgentMonitoringOrchestrator _agentMonitoringOrchestrator;

    public AgentDataController(
        AgentMonitoringOrchestrator agentMonitoringOrchestrator,
        AgentRuntimeCache agentRuntimeCache)
    {
        _agentMonitoringOrchestrator = agentMonitoringOrchestrator;
        _agentRuntimeCache = agentRuntimeCache;
    }

    [HttpGet]
    public AgentData Get()
    {
        var configuration = _agentRuntimeCache.CurrentConfiguration;
        var lastRefresh = _agentRuntimeCache.LastConfigurationRefreshAtUtc;

        if (configuration is null)
        {
            return new AgentData
            {
                LastConfigurationRefreshAtUtc = lastRefresh
            };
        }

        return new AgentData
        {
            AgentId = configuration.Agent.Id.ToString(),
            AgentName = configuration.Agent.Name,
            LastConfigurationRefreshAtUtc = lastRefresh,
            ShellyDeviceCount = configuration.ShellyDevices.Count,
            MonitoredDeviceCount = configuration.MonitoredDevices.Count
        };
    }

    [HttpPost("run-cycle")]
    public async Task<ActionResult<AgentData>> RunCycle(CancellationToken cancellationToken)
    {
        var cycle = await _agentMonitoringOrchestrator.ExecuteCycleAsync(refreshConfiguration: true, cancellationToken);
        _agentRuntimeCache.Update(cycle.Configuration);
        _agentRuntimeCache.MarkCycleExecuted();

        var response = new AgentData
        {
            AgentId = cycle.Configuration.Agent.Id.ToString(),
            AgentName = cycle.Configuration.Agent.Name,
            LastConfigurationRefreshAtUtc = _agentRuntimeCache.LastConfigurationRefreshAtUtc,
            LastMonitoringCycleAtUtc = cycle.Result.ExecutedAtUtc,
            ShellyDeviceCount = cycle.Configuration.ShellyDevices.Count,
            MonitoredDeviceCount = cycle.Configuration.MonitoredDevices.Count,
            SubmittedSensorReadingCount = cycle.Result.SubmittedSensorReadings.Count,
            ReachableMonitoredDeviceCount = cycle.Result.PingResults.Count(x => x.IsReachable),
            UnreachableMonitoredDeviceCount = cycle.Result.PingResults.Count(x => !x.IsReachable)
        };

        return Ok(response);
    }

    [HttpPost("shelly-webhook/{shellyDeviceId:guid}")]
    public async Task<ActionResult> ShellyWebhook(Guid shellyDeviceId, [FromBody] VirtualShellyStatusResponse payload, CancellationToken cancellationToken)
    {
        var configuration = _agentRuntimeCache.CurrentConfiguration;
        if (configuration is null || configuration.ShellyDevices.All(x => x.Id != shellyDeviceId))
        {
            return NotFound();
        }

        await _agentMonitoringOrchestrator.ProcessWebhookAsync(shellyDeviceId, payload, cancellationToken);
        return Accepted();
    }
}
