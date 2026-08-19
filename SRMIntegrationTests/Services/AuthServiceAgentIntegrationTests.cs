using Microsoft.AspNetCore.Identity;
using SRMAuth.Data;
using SRMAuth.Services;
using SRMIntegrationTests.TestHelpers;
using SRMShared.DTOs.Auth;
using SRMShared.Entities;

namespace SRMIntegrationTests.Services;

[TestFixture]
public class AuthServiceAgentIntegrationTests
{
    [SetUp]
    public void ResetDatabase()
    {
        using var context = AuthSqlServerDbContextFactory.CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Test]
    public async Task LoginAgentAsync_ShouldReturnToken_ForValidAgentCredentials()
    {
        using var context = AuthSqlServerDbContextFactory.CreateContext();
        var passwordHasher = new PasswordHasher<AuthUser>();

        var agentCredential = new AgentCredential
        {
            AgentId = Guid.NewGuid(),
            ClientIdentifier = "agent-client-01",
            SecretHash = passwordHasher.HashPassword(new AuthUser { Username = "agent-client-01" }, "AgentSecret123!"),
            IsActive = true
        };

        context.AgentCredentials.Add(agentCredential);
        await context.SaveChangesAsync();

        var service = new AuthService(
            context,
            passwordHasher,
            new FakeJwtTokenService(),
            AuthCurrentUserContextFactory.Create());

        var result = await service.LoginAgentAsync(new AgentLoginRequestDto
        {
            ClientIdentifier = "agent-client-01",
            ClientSecret = "AgentSecret123!"
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.AccessToken, Does.StartWith("agent-token-"));
        Assert.That(result.AgentId, Is.EqualTo(agentCredential.AgentId));
    }
}
