using SRMAuth.Services.Interfaces;
using SRMShared.Entities;

namespace SRMIntegrationTests.TestHelpers;

internal class FakeJwtTokenService : IJwtTokenService
{
    public (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateUserAccessToken(AuthUser user, IEnumerable<string> roles, Guid? customerId)
    {
        return ($"user-token-{user.Username}", DateTime.UtcNow.AddHours(1), $"user-jti-{user.Username}");
    }

    public (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateAgentAccessToken(AgentCredential agentCredential)
    {
        return ($"agent-token-{agentCredential.ClientIdentifier}", DateTime.UtcNow.AddHours(1), $"agent-jti-{agentCredential.ClientIdentifier}");
    }
}
