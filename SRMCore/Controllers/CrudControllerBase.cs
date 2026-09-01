using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRMCore.Mappings.Interfaces;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Controllers;

[ApiController]
[Authorize(Roles = "SystemAdmin,Employee,CustomerAdmin,Customer")]
public abstract class CrudControllerBase<TEntity, TCreateDto, TUpdateDto, TReadDto>(
    ICrudService<TEntity> service,
    ICrudDtoMapper<TEntity, TCreateDto, TUpdateDto, TReadDto> mapper) : ControllerBase
    where TEntity : BaseEntity
{
    protected readonly ICrudService<TEntity> Service = service;
    protected readonly ICrudDtoMapper<TEntity, TCreateDto, TUpdateDto, TReadDto> Mapper = mapper;

    [HttpGet]
    public virtual async Task<ActionResult<IEnumerable<TReadDto>>> GetAll()
    {
        var entities = await Service.GetAllAsync();
        return Ok(entities.Select(Mapper.ToReadDto));
    }

    [HttpGet("{id:guid}")]
    public virtual async Task<ActionResult<TReadDto>> GetById(Guid id)
    {
        var entity = await Service.GetByIdAsync(id);
        return entity is null ? NotFound() : Ok(Mapper.ToReadDto(entity));
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,Employee")]
    public virtual async Task<ActionResult<TReadDto>> Create(TCreateDto dto)
    {
        var entity = await Service.CreateAsync(Mapper.ToEntity(dto));
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, Mapper.ToReadDto(entity));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SystemAdmin,Employee")]
    public virtual async Task<ActionResult<TReadDto>> Update(Guid id, TUpdateDto dto)
    {
        var entity = await Service.UpdateAsync(id, Mapper.ToEntity(dto));
        return entity is null ? NotFound() : Ok(Mapper.ToReadDto(entity));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SystemAdmin,Employee")]
    public virtual async Task<IActionResult> Delete(Guid id)
    {
        return await Service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
