using NUnit.Framework;
using SRMCore.Data;
using SRMCore.Services.Interfaces;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Services;

public abstract class CrudServiceTestBase<TEntity> where TEntity : BaseEntity
{
    protected abstract ICrudService<TEntity> CreateService(SrmCoreDbContext context);
    protected abstract TEntity CreateEntity();
    protected abstract TEntity CreateUpdatedEntity();
    protected abstract void AssertEntityUpdated(TEntity entity);

    [Test]
    public async Task CreateAsync_ShouldAssignIdAndTimestamps()
    {
        using var context = DbContextFactory.CreateContext();
        var service = CreateService(context);

        var created = await service.CreateAsync(CreateEntity());

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.CreatedAtUtc, Is.Not.EqualTo(default(DateTime)));
        Assert.That(created.UpdatedAtUtc, Is.EqualTo(created.CreatedAtUtc));
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateExistingEntity()
    {
        using var context = DbContextFactory.CreateContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateEntity());
        var originalCreatedAt = created.CreatedAtUtc;

        var updated = await service.UpdateAsync(created.Id, CreateUpdatedEntity());

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Id, Is.EqualTo(created.Id));
        Assert.That(updated.CreatedAtUtc, Is.EqualTo(originalCreatedAt));
        Assert.That(updated.UpdatedAtUtc, Is.GreaterThanOrEqualTo(originalCreatedAt));
        AssertEntityUpdated(updated);
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveExistingEntity()
    {
        using var context = DbContextFactory.CreateContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateEntity());

        var deleted = await service.DeleteAsync(created.Id);
        var fetched = await service.GetByIdAsync(created.Id);

        Assert.That(deleted, Is.True);
        Assert.That(fetched, Is.Null);
    }
}
