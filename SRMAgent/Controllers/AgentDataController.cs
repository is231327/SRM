using Microsoft.AspNetCore.Mvc;
using SRMAgent.Models.Shelly;
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
