using Microsoft.AspNetCore.Mvc;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.MonitoredDevice;
using SRMShared.Entities;

namespace SRMCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonitoredDevicesController(
    IMonitoredDeviceService service,
    ICrudDtoMapper<MonitoredDevice, MonitoredDeviceCreateDto, MonitoredDeviceUpdateDto, MonitoredDeviceReadDto> mapper)
    : CrudControllerBase<MonitoredDevice, MonitoredDeviceCreateDto, MonitoredDeviceUpdateDto, MonitoredDeviceReadDto>(service, mapper)
{
}
