using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SRMCore.Controllers;
using SRMCore.Data;
using SRMCore.Mappings;
using SRMCore.Mappings.Interfaces;
using SRMCore.Security;
using SRMCore.Services;
using SRMCore.Services.Interfaces;
using SRMShared.Auth;
using SRMShared.DTOs.ServerRoom;
using SRMShared.Entities;

namespace SRMIntegrationTests.Security;

public class CoreAuthorizationHttpIntegrationTests
{
    [TestCase("SystemAdmin", 2, HttpStatusCode.Created)]
    [TestCase("Employee", 2, HttpStatusCode.Created)]
    [TestCase("CustomerAdmin", 1, HttpStatusCode.Forbidden)]
    [TestCase("Customer", 1, HttpStatusCode.Forbidden)]
    public async Task ServerRoomEndpoints_EnforceRoleAndCustomerOwnership(
        string role,
        int expectedVisibleRooms,
        HttpStatusCode expectedMutationStatus)
    {
        var customerId = Guid.NewGuid();
        await using var app = await CreateAppAsync(customerId);
        var client = AuthenticatedClient(app, role, customerId);

        var rooms = await client.GetFromJsonAsync<List<ServerRoomReadDto>>("/api/serverrooms");
        var createResponse = await client.PostAsJsonAsync("/api/serverrooms", new ServerRoomCreateDto
        {
            CustomerId = customerId,
            Name = "Created through HTTP",
            LocationDescription = "Test",
            TemperatureWarningThreshold = 25,
            TemperatureCriticalThreshold = 30,
            MonitoringEnabled = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(rooms, Has.Count.EqualTo(expectedVisibleRooms));
            Assert.That(createResponse.StatusCode, Is.EqualTo(expectedMutationStatus));
        });
    }

    [Test]
    public async Task ServerRoomEndpoints_RejectAnonymousAndAgentRequests()
    {
        var customerId = Guid.NewGuid();
        await using var app = await CreateAppAsync(customerId);

        var anonymousStatus = (await app.GetTestClient().GetAsync("/api/serverrooms")).StatusCode;
        var agentStatus = (await AuthenticatedClient(app, "Agent", customerId).GetAsync("/api/serverrooms")).StatusCode;

        Assert.Multiple(() =>
        {
            Assert.That(anonymousStatus, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(agentStatus, Is.EqualTo(HttpStatusCode.Forbidden));
        });
    }

    private static async Task<WebApplication> CreateAppAsync(Guid customerId)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var databaseName = Guid.NewGuid().ToString("N");
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddDbContext<SrmCoreDbContext>(options => options.UseInMemoryDatabase(databaseName));
        builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        builder.Services.AddScoped<IServerRoomService, ServerRoomService>();
        builder.Services.AddScoped<ICrudDtoMapper<ServerRoom, ServerRoomCreateDto, ServerRoomUpdateDto, ServerRoomReadDto>, ServerRoomDtoMapper>();
        builder.Services.AddControllers().AddApplicationPart(typeof(ServerRoomsController).Assembly);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SrmCoreDbContext>();
        var otherCustomerId = Guid.NewGuid();
        context.Customers.AddRange(
            new Customer { Id = customerId, Name = "Own", ExternalReference = "OWN", IsActive = true },
            new Customer { Id = otherCustomerId, Name = "Other", ExternalReference = "OTHER", IsActive = true });
        context.ServerRooms.AddRange(
            new ServerRoom { CustomerId = customerId, Name = "Own Room", MonitoringEnabled = true },
            new ServerRoom { CustomerId = otherCustomerId, Name = "Other Room", MonitoringEnabled = true });
        await context.SaveChangesAsync();
        return app;
    }

    private static HttpClient AuthenticatedClient(WebApplication app, string role, Guid customerId)
    {
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-Customer", customerId.ToString());
        return client;
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new(ClaimTypes.Role, role.ToString())
            };
            if (Request.Headers.TryGetValue("X-Test-Customer", out var customerId))
            {
                claims.Add(new Claim(AuthClaimTypes.CustomerId, customerId.ToString()));
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
        }
    }
}
