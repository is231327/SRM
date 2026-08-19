using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using SRMCore.Controllers;
using SRMCore.Mappings;
using SRMCore.Services;
using SRMShared.DTOs.AgentRuntime;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Controllers;

[TestFixture]
public class AgentRuntimeControllerTests
{
    [Test]
    public async Task GetConfiguration_ShouldReturnOk_WithRuntimeConfiguration()
    {
        using var context = DbContextFactory.CreateContext();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Customer", ContactEmail = "customer@example.com", ContactPhone = "123" };
        var serverRoom = new ServerRoom { Id = Guid.NewGuid(), CustomerId = customer.Id, Name = "Room", LocationDescription = "A", TemperatureWarningThreshold = 25, TemperatureCriticalThreshold = 30, MonitoringEnabled = true };
        var agent = new Agent { Id = Guid.NewGuid(), ServerRoomId = serverRoom.Id, Name = "Agent", ApiKeyReference = "ref", Version = "1.0", LastKnownIpAddress = "127.0.0.1", IsActive = true };

        context.Customers.Add(customer);
        context.ServerRooms.Add(serverRoom);
        context.Agents.Add(agent);
        await context.SaveChangesAsync();

        var currentUserContext = new FakeCoreCurrentUserContext
        {
            IsSystemAdmin = false,
            IsAgent = true,
            AgentId = agent.Id
        };

        var controller = new AgentRuntimeController(
            new AgentRuntimeService(
                context,
                currentUserContext,
                new AgentDtoMapper(),
                new ShellyDeviceDtoMapper(),
                new MonitoredDeviceDtoMapper()));

        var result = await controller.GetConfiguration();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        Assert.That(okResult.Value, Is.InstanceOf<AgentRuntimeConfigurationDto>());
    }
}
