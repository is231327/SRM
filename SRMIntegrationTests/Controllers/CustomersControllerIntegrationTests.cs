using Microsoft.AspNetCore.Mvc;
using SRMCore.Controllers;
using SRMCore.Mappings;
using SRMCore.Services;
using SRMShared.DTOs.Customer;
using SRMIntegrationTests.TestHelpers;

namespace SRMIntegrationTests.Controllers;

[TestFixture]
public class CustomersControllerIntegrationTests
{
    [SetUp]
    public void ResetDatabase()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Test]
    public async Task CreateAndGetById_ShouldRoundTripThroughSqlServer()
    {
        Guid createdId;

        using (var createContext = SqlServerDbContextFactory.CreateContext())
        {
            var controller = new CustomersController(new CustomerService(createContext, CoreCurrentUserContextFactory.Create()), new CustomerDtoMapper());

            var createResult = await controller.Create(new CustomerCreateDto
            {
                Name = "Controller Integration Customer",
                ExternalReference = "CTRL-001",
                ContactEmail = "controller@example.com",
                ContactPhone = "789",
                IsActive = true
            });

            Assert.That(createResult.Result, Is.InstanceOf<CreatedAtActionResult>());
            var createdDto = (CustomerReadDto)((CreatedAtActionResult)createResult.Result!).Value!;
            createdId = createdDto.Id;
        }

        using var readContext = SqlServerDbContextFactory.CreateContext();
        var readController = new CustomersController(new CustomerService(readContext, CoreCurrentUserContextFactory.Create()), new CustomerDtoMapper());
        var getResult = await readController.GetById(createdId);

        Assert.That(getResult.Result, Is.InstanceOf<OkObjectResult>());
        var dto = (CustomerReadDto)((OkObjectResult)getResult.Result!).Value!;
        Assert.That(dto.Name, Is.EqualTo("Controller Integration Customer"));
    }
}
