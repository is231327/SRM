using Microsoft.AspNetCore.Mvc;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.Agent;
using SRMShared.Entities;

namespace SRMCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController(
    IAgentService service,
    ICrudDtoMapper<Agent, AgentCreateDto, AgentUpdateDto, AgentReadDto> mapper)
    : CrudControllerBase<Agent, AgentCreateDto, AgentUpdateDto, AgentReadDto>(service, mapper)
{
}
