using SRMShared.Entities;

namespace SRMCore.Services.Interfaces;

public interface ICrudService<TEntity> where TEntity : BaseEntity
{
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<TEntity> CreateAsync(TEntity entity);
    Task<TEntity?> UpdateAsync(Guid id, TEntity entity);
    Task<bool> DeleteAsync(Guid id);
}
