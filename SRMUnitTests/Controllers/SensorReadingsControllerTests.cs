using Microsoft.AspNetCore.Mvc;
using SRMCore.Controllers;
using SRMCore.Mappings;
using SRMCore.Services;
using SRMShared.DTOs.SensorReading;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Controllers;

[TestFixture]
public class SensorReadingsControllerTests : CrudControllerTestBase<SensorReadingCreateDto, SensorReadingReadDto>
{
    protected override SensorReadingCreateDto CreateDto() => new()
    {
        ShellyDeviceId = Guid.NewGuid(),
        TemperatureCelsius = 22.5f,
        BatteryPercent = 80,
        Brightness = 100,
        DoorOpen = false,
        RecordedAtUtc = DateTime.UtcNow
    };

    protected override async Task<ActionResult<SensorReadingReadDto>> ExecuteCreateAsync(SensorReadingCreateDto dto)
    {
        using var context = DbContextFactory.CreateContext();
        return await new SensorReadingsController(new SensorReadingService(context, CoreCurrentUserContextFactory.Create()), new SensorReadingDtoMapper()).Create(dto);
    }

    protected override async Task<IActionResult> ExecuteDeleteAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new SensorReadingsController(new SensorReadingService(context, CoreCurrentUserContextFactory.Create()), new SensorReadingDtoMapper());
        var created = (CreatedAtActionResult)(await controller.Create(CreateDto())).Result!;
        var createdId = (Guid)created.Value!.GetType().GetProperty("Id")!.GetValue(created.Value)!;
        return await controller.Delete(createdId);
    }

    protected override async Task<ActionResult<IEnumerable<SensorReadingReadDto>>> ExecuteGetAllAsync()
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new SensorReadingsController(new SensorReadingService(context, CoreCurrentUserContextFactory.Create()), new SensorReadingDtoMapper());
        await controller.Create(CreateDto());
        return await controller.GetAll();
    }

    protected override async Task<ActionResult<SensorReadingReadDto>> ExecuteGetByIdAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        return await new SensorReadingsController(new SensorReadingService(context, CoreCurrentUserContextFactory.Create()), new SensorReadingDtoMapper()).GetById(id);
    }
}
