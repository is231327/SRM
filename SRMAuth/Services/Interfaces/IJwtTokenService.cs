using SRMShared.Entities;

namespace SRMAuth.Services.Interfaces;

public interface IJwtTokenService
{
    (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateUserAccessToken(AuthUser user, IEnumerable<string> roles, Guid? customerId);
    (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateAgentAccessToken(AgentCredential agentCredential);
}
