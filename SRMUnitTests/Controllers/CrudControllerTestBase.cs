using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace SRMUnitTests.Controllers;

public abstract class CrudControllerTestBase<TCreateDto, TReadDto>
{
    protected abstract Task<ActionResult<TReadDto>> ExecuteCreateAsync(TCreateDto dto);
    protected abstract Task<ActionResult<TReadDto>> ExecuteGetByIdAsync(Guid id);
    protected abstract Task<ActionResult<IEnumerable<TReadDto>>> ExecuteGetAllAsync();
    protected abstract Task<IActionResult> ExecuteDeleteAsync(Guid id);
    protected abstract TCreateDto CreateDto();

    [Test]
    public async Task Create_ShouldReturnCreatedAtActionWithReadDto()
    {
        var result = await ExecuteCreateAsync(CreateDto());

        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result.Result!;
        Assert.That(createdResult.ActionName, Is.EqualTo("GetById"));
        Assert.That(createdResult.Value, Is.InstanceOf<TReadDto>());

        var idProperty = createdResult.Value!.GetType().GetProperty("Id");
        Assert.That(idProperty, Is.Not.Null);
        Assert.That((Guid)idProperty!.GetValue(createdResult.Value)!, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task GetById_ShouldReturnNotFoundForUnknownEntity()
    {
        var result = await ExecuteGetByIdAsync(Guid.NewGuid());

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetAll_ShouldReturnOkResult()
    {
        await ExecuteCreateAsync(CreateDto());

        var result = await ExecuteGetAllAsync();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Delete_ShouldReturnNoContentForExistingEntity()
    {
        var createResult = await ExecuteCreateAsync(CreateDto());
        var created = (CreatedAtActionResult)createResult.Result!;
        var id = (Guid)created.Value!.GetType().GetProperty("Id")!.GetValue(created.Value)!;

        var result = await ExecuteDeleteAsync(id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }
}
