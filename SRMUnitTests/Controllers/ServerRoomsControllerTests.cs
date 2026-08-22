using Microsoft.AspNetCore.Mvc;
using SRMCore.Controllers;
using SRMCore.Mappings;
using SRMCore.Services;
using SRMShared.DTOs.ServerRoom;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Controllers;

[TestFixture]
public class ServerRoomsControllerTests : CrudControllerTestBase<ServerRoomCreateDto, ServerRoomReadDto>
{
    protected override ServerRoomCreateDto CreateDto() => new()
    {
        CustomerId = Guid.NewGuid(),
        Name = "Room A",
        LocationDescription = "First floor",
        TemperatureWarningThreshold = 25,
        TemperatureCriticalThreshold = 30,
        MonitoringEnabled = true
    };

    protected override async Task<ActionResult<ServerRoomReadDto>> ExecuteCreateAsync(ServerRoomCreateDto dto)
    {
        using var context = DbContextFactory.CreateContext();
        return await new ServerRoomsController(new ServerRoomService(context, CoreCurrentUserContextFactory.Create()), new ServerRoomDtoMapper()).Create(dto);
    }

    protected override async Task<IActionResult> ExecuteDeleteAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new ServerRoomsController(new ServerRoomService(context, CoreCurrentUserContextFactory.Create()), new ServerRoomDtoMapper());
        var created = (CreatedAtActionResult)(await controller.Create(CreateDto())).Result!;
        var createdId = (Guid)created.Value!.GetType().GetProperty("Id")!.GetValue(created.Value)!;
        return await controller.Delete(createdId);
    }

    protected override async Task<ActionResult<IEnumerable<ServerRoomReadDto>>> ExecuteGetAllAsync()
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new ServerRoomsController(new ServerRoomService(context, CoreCurrentUserContextFactory.Create()), new ServerRoomDtoMapper());
        await controller.Create(CreateDto());
        return await controller.GetAll();
    }

    protected override async Task<ActionResult<ServerRoomReadDto>> ExecuteGetByIdAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        return await new ServerRoomsController(new ServerRoomService(context, CoreCurrentUserContextFactory.Create()), new ServerRoomDtoMapper()).GetById(id);
    }
}
