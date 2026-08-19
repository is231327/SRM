using SRMCore.Services;
using SRMShared.Entities;
using SRMIntegrationTests.TestHelpers;

namespace SRMIntegrationTests.Services;

[TestFixture]
public class CustomerServiceIntegrationTests
{
    [SetUp]
    public void ResetDatabase()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Test]
    public async Task CreateAsync_ShouldPersistCustomerInSqlServer()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        var service = new CustomerService(context, CoreCurrentUserContextFactory.Create());

        var created = await service.CreateAsync(new Customer
        {
            Name = "Integration Customer",
            ExternalReference = "INT-001",
            ContactEmail = "integration@example.com",
            ContactPhone = "123",
            IsActive = true
        });

        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));

        using var verificationContext = SqlServerDbContextFactory.CreateContext();
        var persisted = await verificationContext.Customers.FindAsync(created.Id);
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.Name, Is.EqualTo("Integration Customer"));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveCustomerFromSqlServer()
    {
        Guid customerId;

        using (var setupContext = SqlServerDbContextFactory.CreateContext())
        {
            var service = new CustomerService(setupContext, CoreCurrentUserContextFactory.Create());
            var created = await service.CreateAsync(new Customer
            {
                Name = "Delete Customer",
                ExternalReference = "INT-DEL",
                ContactEmail = "delete@example.com",
                ContactPhone = "456",
                IsActive = true
            });
            customerId = created.Id;
        }

        using (var deleteContext = SqlServerDbContextFactory.CreateContext())
        {
            var service = new CustomerService(deleteContext, CoreCurrentUserContextFactory.Create());
            var deleted = await service.DeleteAsync(customerId);
            Assert.That(deleted, Is.True);
        }

        using var verificationContext = SqlServerDbContextFactory.CreateContext();
        var persisted = await verificationContext.Customers.FindAsync(customerId);
        Assert.That(persisted, Is.Null);
    }
}
