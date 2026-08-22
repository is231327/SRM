using Microsoft.AspNetCore.Mvc;
using SRMCore.Controllers;
using SRMCore.Mappings;
using SRMCore.Services;
using SRMShared.DTOs.ShellyDevice;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Controllers;

[TestFixture]
public class ShellyDevicesControllerTests : CrudControllerTestBase<ShellyDeviceCreateDto, ShellyDeviceReadDto>
{
    protected override ShellyDeviceCreateDto CreateDto() => new()
    {
        AgentId = Guid.NewGuid(),
        Name = "Shelly A",
        DeviceType = "DoorWindow2",
        BaseUrl = "http://shelly-a",
        MacAddress = "AA:BB:CC:DD:EE:01",
        FirmwareVersion = "1.0",
        IsVirtual = true,
        IsActive = true
    };

    protected override async Task<ActionResult<ShellyDeviceReadDto>> ExecuteCreateAsync(ShellyDeviceCreateDto dto)
    {
        using var context = DbContextFactory.CreateContext();
        return await new ShellyDevicesController(new ShellyDeviceService(context, CoreCurrentUserContextFactory.Create()), new ShellyDeviceDtoMapper()).Create(dto);
    }

    protected override async Task<IActionResult> ExecuteDeleteAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new ShellyDevicesController(new ShellyDeviceService(context, CoreCurrentUserContextFactory.Create()), new ShellyDeviceDtoMapper());
        var created = (CreatedAtActionResult)(await controller.Create(CreateDto())).Result!;
        var createdId = (Guid)created.Value!.GetType().GetProperty("Id")!.GetValue(created.Value)!;
        return await controller.Delete(createdId);
    }

    protected override async Task<ActionResult<IEnumerable<ShellyDeviceReadDto>>> ExecuteGetAllAsync()
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new ShellyDevicesController(new ShellyDeviceService(context, CoreCurrentUserContextFactory.Create()), new ShellyDeviceDtoMapper());
        await controller.Create(CreateDto());
        return await controller.GetAll();
    }

    protected override async Task<ActionResult<ShellyDeviceReadDto>> ExecuteGetByIdAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        return await new ShellyDevicesController(new ShellyDeviceService(context, CoreCurrentUserContextFactory.Create()), new ShellyDeviceDtoMapper()).GetById(id);
    }
}
