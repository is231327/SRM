using Microsoft.EntityFrameworkCore;
using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;

namespace SRMCore.Services;

public class CrudService<TEntity>(SrmCoreDbContext dbContext) : ICrudService<TEntity>
    where TEntity : BaseEntity
{
    protected readonly SrmCoreDbContext DbContext = dbContext;

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await DbContext.Set<TEntity>().ToListAsync();
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await DbContext.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id);
    }

    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        entity.Id = Guid.NewGuid();
        DbContext.Set<TEntity>().Add(entity);
        await DbContext.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<TEntity?> UpdateAsync(Guid id, TEntity entity)
    {
        TEntity? existing = await DbContext.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return null;
        }

        entity.Id = id;
        entity.CreatedAtUtc = existing.CreatedAtUtc;
        entity.UpdatedAtUtc = existing.UpdatedAtUtc;
        DbContext.Entry(existing).CurrentValues.SetValues(entity);
        await DbContext.SaveChangesAsync();
        return existing;
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        TEntity? existing = await DbContext.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return false;
        }

        DbContext.Set<TEntity>().Remove(existing);
        await DbContext.SaveChangesAsync();
        return true;
    }
}
