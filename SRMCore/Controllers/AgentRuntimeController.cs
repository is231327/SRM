using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.AgentRuntime;

namespace SRMCore.Controllers;

[ApiController]
[Route("api/agent-runtime")]
[Authorize(Roles = "Agent")]
public class AgentRuntimeController(IAgentRuntimeService agentRuntimeService) : ControllerBase
{
    [HttpGet("configuration")]
    public async Task<ActionResult<AgentRuntimeConfigurationDto>> GetConfiguration()
    {
        var configuration = await agentRuntimeService.GetRuntimeConfigurationAsync();
        return configuration is null ? NotFound() : Ok(configuration);
    }
}
