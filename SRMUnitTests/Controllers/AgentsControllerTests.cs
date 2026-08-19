using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using SRMCore.Controllers;
using SRMCore.Mappings;
using SRMCore.Services;
using SRMShared.DTOs.Agent;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Controllers;

[TestFixture]
public class AgentsControllerTests : CrudControllerTestBase<AgentCreateDto, AgentReadDto>
{
    protected override AgentCreateDto CreateDto() => new()
    {
        ServerRoomId = Guid.NewGuid(),
        Name = "Agent A",
        ApiKeyReference = "key-a",
        Version = "1.0.0",
        LastKnownIpAddress = "192.168.0.10",
        LastSeenAtUtc = DateTime.UtcNow,
        IsActive = true
    };

    protected override async Task<ActionResult<AgentReadDto>> ExecuteCreateAsync(AgentCreateDto dto)
    {
        using var context = DbContextFactory.CreateContext();
        return await new AgentsController(new AgentService(context, CoreCurrentUserContextFactory.Create()), new AgentDtoMapper()).Create(dto);
    }

    protected override async Task<IActionResult> ExecuteDeleteAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new AgentsController(new AgentService(context, CoreCurrentUserContextFactory.Create()), new AgentDtoMapper());
        var created = (CreatedAtActionResult)(await controller.Create(CreateDto())).Result!;
        var createdId = (Guid)created.Value!.GetType().GetProperty("Id")!.GetValue(created.Value)!;
        return await controller.Delete(createdId);
    }

    protected override async Task<ActionResult<IEnumerable<AgentReadDto>>> ExecuteGetAllAsync()
    {
        using var context = DbContextFactory.CreateContext();
        var controller = new AgentsController(new AgentService(context, CoreCurrentUserContextFactory.Create()), new AgentDtoMapper());
        await controller.Create(CreateDto());
        return await controller.GetAll();
    }

    protected override async Task<ActionResult<AgentReadDto>> ExecuteGetByIdAsync(Guid id)
    {
        using var context = DbContextFactory.CreateContext();
        return await new AgentsController(new AgentService(context, CoreCurrentUserContextFactory.Create()), new AgentDtoMapper()).GetById(id);
    }
}
