using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using SRMCore.Controllers;
using SRMCore.Mappings;
using SRMCore.Services;
using SRMShared.DTOs.Customer;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Controllers;

[TestFixture]
public class CustomersControllerTests
{
    [Test]
    public async Task Create_ShouldReturnCreatedAtActionWithReadDto()
    {
        using var context = DbContextFactory.CreateContext();
        var service = new CustomerService(context);
        var controller = new CustomersController(service, new CustomerDtoMapper());

        var result = await controller.Create(new CustomerCreateDto
        {
            Name = "Customer A",
            ExternalReference = "CUS-001",
            ContactEmail = "a@example.com",
            ContactPhone = "123",
            IsActive = true
        });

        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result.Result!;
        Assert.That(createdResult.ActionName, Is.EqualTo("GetById"));
        Assert.That(createdResult.Value, Is.InstanceOf<CustomerReadDto>());

        var dto = (CustomerReadDto)createdResult.Value!;
        Assert.That(dto.Name, Is.EqualTo("Customer A"));
        Assert.That(dto.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task GetById_ShouldReturnNotFoundForUnknownCustomer()
    {
        using var context = DbContextFactory.CreateContext();
        var service = new CustomerService(context);
        var controller = new CustomersController(service, new CustomerDtoMapper());

        var result = await controller.GetById(Guid.NewGuid());

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetAll_ShouldReturnMappedReadDtos()
    {
        using var context = DbContextFactory.CreateContext();
        context.Customers.Add(new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Customer A",
            ExternalReference = "CUS-001",
            ContactEmail = "a@example.com",
            ContactPhone = "123",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        context.Customers.Add(new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Customer B",
            ExternalReference = "CUS-002",
            ContactEmail = "b@example.com",
            ContactPhone = "456",
            IsActive = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new CustomerService(context);
        var controller = new CustomersController(service, new CustomerDtoMapper());

        var result = await controller.GetAll();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        Assert.That(okResult.Value, Is.InstanceOf<IEnumerable<CustomerReadDto>>());

        var dtos = ((IEnumerable<CustomerReadDto>)okResult.Value!).ToList();
        Assert.That(dtos, Has.Count.EqualTo(2));
        Assert.That(dtos.All(x => x.Id != Guid.Empty), Is.True);
    }

    [Test]
    public async Task Delete_ShouldReturnNoContentForExistingCustomer()
    {
        using var context = DbContextFactory.CreateContext();
        var service = new CustomerService(context);
        var controller = new CustomersController(service, new CustomerDtoMapper());
        var created = await service.CreateAsync(new Customer
        {
            Name = "Delete Me",
            ExternalReference = "DEL-1",
            ContactEmail = "delete@example.com",
            ContactPhone = "123",
            IsActive = true
        });

        var result = await controller.Delete(created.Id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }
}
