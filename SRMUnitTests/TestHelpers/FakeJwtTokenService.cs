using SRMAuth.Services.Interfaces;
using SRMShared.Entities;

namespace SRMUnitTests.TestHelpers;

internal class FakeJwtTokenService : IJwtTokenService
{
    public (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateUserAccessToken(AuthUser user, IEnumerable<string> roles, Guid? customerId)
    {
        return ("fake-user-token", DateTime.UtcNow.AddMinutes(10), "fake-user-jti");
    }

    public (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateAgentAccessToken(AgentCredential agentCredential)
    {
        return ("fake-agent-token", DateTime.UtcNow.AddMinutes(10), "fake-agent-jti");
    }
}
