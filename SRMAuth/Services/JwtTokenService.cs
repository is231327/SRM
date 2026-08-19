using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SRMAuth.Configuration;
using SRMAuth.Services.Interfaces;
using SRMShared.Auth;
using SRMShared.Entities;

namespace SRMAuth.Services;

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string AccessToken, DateTime ExpiresAtUtc) CreateUserAccessToken(AuthUser user, IEnumerable<string> roles, Guid? customerId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        if (customerId.HasValue)
        {
            claims.Add(new Claim(AuthClaimTypes.CustomerId, customerId.Value.ToString()));
        }

        return CreateToken(claims);
    }

    public (string AccessToken, DateTime ExpiresAtUtc) CreateAgentAccessToken(AgentCredential agentCredential)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, agentCredential.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, agentCredential.ClientIdentifier),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, agentCredential.Id.ToString()),
            new(ClaimTypes.Name, agentCredential.ClientIdentifier),
            new(ClaimTypes.Role, AuthRoles.ToName(AuthRoleType.Agent)),
            new(AuthClaimTypes.AgentId, agentCredential.AgentId.ToString()),
            new(AuthClaimTypes.Scope, "agent.api")
        };

        return CreateToken(claims);
    }

    private (string AccessToken, DateTime ExpiresAtUtc) CreateToken(IEnumerable<Claim> claims)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
