using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using SRMCore.Controllers;
using SRMCore.Mappings;
using SRMCore.Services;
using SRMShared.DTOs.MaintenanceWindow;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Controllers;

[TestFixture]
public class MaintenanceWindowsControllerTests : CrudControllerTestBase<MaintenanceWindowCreateDto, MaintenanceWindowReadDto>
{
    protected override MaintenanceWindowCreateDto CreateDto() => new()
    {
        ServerRoomId = Guid.NewGuid(),
        Title = "Window A",
        StartUtc = DateTime.UtcNow,
        EndUtc = DateTime.UtcNow.AddHours(1),
        Description = "Work"
    };

    protected override async Task<ActionResult<MaintenanceWindowReadDto>> ExecuteCreateAsync(MaintenanceWindowCreateDto dto)
    {
        using var context = DbContextFactory.CreateContext();
        return await new MaintenanceWindowsController(new MaintenanceWindowService(context, CoreCurrentUserContextFactory.Create()), new MaintenanceWindowDtoMapper()).Create(dto);
    }

    protected override async Task<IActionResult> ExecuteDeleteAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new MaintenanceWindowsController(new MaintenanceWindowService(context, CoreCurrentUserContextFactory.Create()), new MaintenanceWindowDtoMapper());
        var created = (CreatedAtActionResult)(await controller.Create(CreateDto())).Result!;
        var createdId = (Guid)created.Value!.GetType().GetProperty("Id")!.GetValue(created.Value)!;
        return await controller.Delete(createdId);
    }

    protected override async Task<ActionResult<IEnumerable<MaintenanceWindowReadDto>>> ExecuteGetAllAsync()
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new MaintenanceWindowsController(new MaintenanceWindowService(context, CoreCurrentUserContextFactory.Create()), new MaintenanceWindowDtoMapper());
        await controller.Create(CreateDto());
        return await controller.GetAll();
    }

    protected override async Task<ActionResult<MaintenanceWindowReadDto>> ExecuteGetByIdAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        return await new MaintenanceWindowsController(new MaintenanceWindowService(context, CoreCurrentUserContextFactory.Create()), new MaintenanceWindowDtoMapper()).GetById(id);
    }
}
