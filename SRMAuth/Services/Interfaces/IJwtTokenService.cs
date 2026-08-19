using SRMShared.Entities;

namespace SRMAuth.Services.Interfaces;

public interface IJwtTokenService
{
    (string AccessToken, DateTime ExpiresAtUtc) CreateUserAccessToken(AuthUser user, IEnumerable<string> roles, Guid? customerId);
    (string AccessToken, DateTime ExpiresAtUtc) CreateAgentAccessToken(AgentCredential agentCredential);
}
