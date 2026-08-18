using Microsoft.AspNetCore.Mvc;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services.Interfaces;
using SRMShared.DTOs.ServerRoom;
using SRMShared.Entities;

namespace SRMCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServerRoomsController(
    IServerRoomService service,
    ICrudDtoMapper<ServerRoom, ServerRoomCreateDto, ServerRoomUpdateDto, ServerRoomReadDto> mapper)
    : CrudControllerBase<ServerRoom, ServerRoomCreateDto, ServerRoomUpdateDto, ServerRoomReadDto>(service, mapper)
{
}
