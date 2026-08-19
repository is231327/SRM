using Microsoft.AspNetCore.Mvc;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.MonitoredDevicePingResult;
using SRMShared.Entities;

namespace SRMCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonitoredDevicePingResultsController(
    IMonitoredDevicePingResultService service,
    ICrudDtoMapper<MonitoredDevicePingResult, MonitoredDevicePingResultCreateDto, MonitoredDevicePingResultUpdateDto, MonitoredDevicePingResultReadDto> mapper)
    : CrudControllerBase<MonitoredDevicePingResult, MonitoredDevicePingResultCreateDto, MonitoredDevicePingResultUpdateDto, MonitoredDevicePingResultReadDto>(service, mapper)
{
}
