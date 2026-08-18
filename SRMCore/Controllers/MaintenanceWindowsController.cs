using Microsoft.AspNetCore.Mvc;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.MaintenanceWindow;
using SRMShared.Entities;

namespace SRMCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaintenanceWindowsController(
    IMaintenanceWindowService service,
    ICrudDtoMapper<MaintenanceWindow, MaintenanceWindowCreateDto, MaintenanceWindowUpdateDto, MaintenanceWindowReadDto> mapper)
    : CrudControllerBase<MaintenanceWindow, MaintenanceWindowCreateDto, MaintenanceWindowUpdateDto, MaintenanceWindowReadDto>(service, mapper)
{
}
