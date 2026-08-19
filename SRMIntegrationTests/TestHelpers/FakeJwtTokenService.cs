using SRMAuth.Services.Interfaces;
using SRMShared.Entities;

namespace SRMIntegrationTests.TestHelpers;

internal class FakeJwtTokenService : IJwtTokenService
{
    public (string AccessToken, DateTime ExpiresAtUtc) CreateUserAccessToken(AuthUser user, IEnumerable<string> roles, Guid? customerId)
    {
        return ($"user-token-{user.Username}", DateTime.UtcNow.AddHours(1));
    }

    public (string AccessToken, DateTime ExpiresAtUtc) CreateAgentAccessToken(AgentCredential agentCredential)
    {
        return ($"agent-token-{agentCredential.ClientIdentifier}", DateTime.UtcNow.AddHours(1));
    }
}
