using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRMCore.Mappings;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.Incident;

namespace SRMCore.Controllers;

[ApiController]
[Route("api/incidents")]
[Authorize(Roles = "SystemAdmin,Employee,CustomerAdmin,Customer")]
public class IncidentsController(IIncidentQueryService incidentQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<IncidentReadDto>>> GetAll(
        [FromQuery] bool includeClosed,
        CancellationToken cancellationToken)
    {
        var incidents = await incidentQueryService.GetAllAsync(includeClosed, cancellationToken);
        return Ok(incidents.Select(IncidentReadDtoMapper.ToReadDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IncidentReadDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var incident = await incidentQueryService.GetByIdAsync(id, cancellationToken);
        if (incident is null)
        {
            return NotFound();
        }

        return Ok(IncidentReadDtoMapper.ToReadDto(incident));
    }
}
