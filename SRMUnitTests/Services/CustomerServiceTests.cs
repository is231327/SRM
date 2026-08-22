using SRMCore.Services;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Services;

[TestFixture]
public class CustomerServiceTests
{
    [Test]
    public async Task CreateAsync_ShouldAssignIdAndTimestamps()
    {
        using var context = DbContextFactory.CreateContext();
        var service = new CustomerService(context, CoreCurrentUserContextFactory.Create());
        var customer = new Customer
        {
            Name = "Customer A",
            ExternalReference = "CUS-001",
            ContactEmail = "a@example.com",
            ContactPhone = "+43123456",
            IsActive = true
        };

        var created = await service.CreateAsync(customer);

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.CreatedAtUtc, Is.Not.EqualTo(default(DateTime)));
        Assert.That(created.UpdatedAtUtc, Is.EqualTo(created.CreatedAtUtc));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnPersistedCustomers()
    {
        using var context = DbContextFactory.CreateContext();
        var service = new CustomerService(context, CoreCurrentUserContextFactory.Create());

        await service.CreateAsync(new Customer { Name = "A", ExternalReference = "A-1", ContactEmail = "a@example.com", ContactPhone = "1", IsActive = true });
        await service.CreateAsync(new Customer { Name = "B", ExternalReference = "B-1", ContactEmail = "b@example.com", ContactPhone = "2", IsActive = false });

        var result = (await service.GetAllAsync()).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(x => x.Name), Is.EquivalentTo(new[] { "A", "B" }));
    }

    [Test]
    public async Task UpdateAsync_ShouldModifyExistingCustomerAndRefreshTimestamp()
    {
        using var context = DbContextFactory.CreateContext();
        var service = new CustomerService(context, CoreCurrentUserContextFactory.Create());

        var created = await service.CreateAsync(new Customer
        {
            Name = "Original",
            ExternalReference = "CUS-001",
            ContactEmail = "original@example.com",
            ContactPhone = "123",
            IsActive = true
        });

        var originalCreatedAt = created.CreatedAtUtc;
        var updated = await service.UpdateAsync(created.Id, new Customer
        {
            Name = "Updated",
            ExternalReference = "CUS-002",
            ContactEmail = "updated@example.com",
            ContactPhone = "456",
            IsActive = false
        });

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Id, Is.EqualTo(created.Id));
        Assert.That(updated.Name, Is.EqualTo("Updated"));
        Assert.That(updated.ExternalReference, Is.EqualTo("CUS-002"));
        Assert.That(updated.CreatedAtUtc, Is.EqualTo(originalCreatedAt));
        Assert.That(updated.UpdatedAtUtc, Is.GreaterThanOrEqualTo(originalCreatedAt));
    }

    [Test]
    public async Task UpdateAsync_ShouldReturnNullForUnknownId()
    {
        using var context = DbContextFactory.CreateContext();
        var service = new CustomerService(context, CoreCurrentUserContextFactory.Create());

        var result = await service.UpdateAsync(Guid.NewGuid(), new Customer { Name = "Missing" });

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveExistingCustomer()
    {
        using var context = DbContextFactory.CreateContext();
        var service = new CustomerService(context, CoreCurrentUserContextFactory.Create());
        var created = await service.CreateAsync(new Customer
        {
            Name = "Delete Me",
            ExternalReference = "DEL-1",
            ContactEmail = "delete@example.com",
            ContactPhone = "123",
            IsActive = true
        });

        var deleted = await service.DeleteAsync(created.Id);
        var remaining = await service.GetByIdAsync(created.Id);

        Assert.That(deleted, Is.True);
        Assert.That(remaining, Is.Null);
    }
}
