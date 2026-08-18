using Microsoft.AspNetCore.Mvc;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.SensorReading;
using SRMShared.Entities;

namespace SRMCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorReadingsController(
    ISensorReadingService service,
    ICrudDtoMapper<SensorReading, SensorReadingCreateDto, SensorReadingUpdateDto, SensorReadingReadDto> mapper)
    : CrudControllerBase<SensorReading, SensorReadingCreateDto, SensorReadingUpdateDto, SensorReadingReadDto>(service, mapper)
{
}
