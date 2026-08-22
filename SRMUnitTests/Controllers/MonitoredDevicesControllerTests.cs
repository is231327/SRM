using Microsoft.AspNetCore.Mvc;
using SRMCore.Controllers;
using SRMCore.Mappings;
using SRMCore.Services;
using SRMShared.DTOs.MonitoredDevice;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Controllers;

[TestFixture]
public class MonitoredDevicesControllerTests : CrudControllerTestBase<MonitoredDeviceCreateDto, MonitoredDeviceReadDto>
{
    protected override MonitoredDeviceCreateDto CreateDto() => new()
    {
        AgentId = Guid.NewGuid(),
        DisplayName = "Switch A",
        IpAddress = "10.0.0.1",
        IntervalSeconds = 30,
        TimeoutMilliseconds = 1000,
        FailureThreshold = 3,
        IsActive = true
    };

    protected override async Task<ActionResult<MonitoredDeviceReadDto>> ExecuteCreateAsync(MonitoredDeviceCreateDto dto)
    {
        using var context = DbContextFactory.CreateContext();
        return await new MonitoredDevicesController(new MonitoredDeviceService(context, CoreCurrentUserContextFactory.Create()), new MonitoredDeviceDtoMapper()).Create(dto);
    }

    protected override async Task<IActionResult> ExecuteDeleteAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new MonitoredDevicesController(new MonitoredDeviceService(context, CoreCurrentUserContextFactory.Create()), new MonitoredDeviceDtoMapper());
        var created = (CreatedAtActionResult)(await controller.Create(CreateDto())).Result!;
        var createdId = (Guid)created.Value!.GetType().GetProperty("Id")!.GetValue(created.Value)!;
        return await controller.Delete(createdId);
    }

    protected override async Task<ActionResult<IEnumerable<MonitoredDeviceReadDto>>> ExecuteGetAllAsync()
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new MonitoredDevicesController(new MonitoredDeviceService(context, CoreCurrentUserContextFactory.Create()), new MonitoredDeviceDtoMapper());
        await controller.Create(CreateDto());
        return await controller.GetAll();
    }

    protected override async Task<ActionResult<MonitoredDeviceReadDto>> ExecuteGetByIdAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        return await new MonitoredDevicesController(new MonitoredDeviceService(context, CoreCurrentUserContextFactory.Create()), new MonitoredDeviceDtoMapper()).GetById(id);
    }
}
