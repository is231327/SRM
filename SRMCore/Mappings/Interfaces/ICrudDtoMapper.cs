using SRMShared.Entities;

namespace SRMCore.Mappings.Interfaces;

public interface ICrudDtoMapper<TEntity, TCreateDto, TUpdateDto, TReadDto>
    where TEntity : BaseEntity
{
    TReadDto ToReadDto(TEntity entity);
    TEntity ToEntity(TCreateDto dto);
    TEntity ToEntity(TUpdateDto dto);
}
