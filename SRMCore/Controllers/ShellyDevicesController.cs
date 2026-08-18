using Microsoft.AspNetCore.Mvc;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.ShellyDevice;
using SRMShared.Entities;

namespace SRMCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShellyDevicesController(
    IShellyDeviceService service,
    ICrudDtoMapper<ShellyDevice, ShellyDeviceCreateDto, ShellyDeviceUpdateDto, ShellyDeviceReadDto> mapper)
    : CrudControllerBase<ShellyDevice, ShellyDeviceCreateDto, ShellyDeviceUpdateDto, ShellyDeviceReadDto>(service, mapper)
{
}
