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

    public (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateUserAccessToken(AuthUser user, IEnumerable<string> roles, Guid? customerId, string sessionVersion)
    {
        var tokenJti = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, tokenJti),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(AuthClaimTypes.SessionVersion, sessionVersion),
            new(AuthClaimTypes.MustChangePassword, user.MustChangePassword ? "true" : "false"),
            new(AuthClaimTypes.MfaAuthenticated, "true")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        if (customerId.HasValue)
        {
            claims.Add(new Claim(AuthClaimTypes.CustomerId, customerId.Value.ToString()));
        }

        return CreateToken(claims, tokenJti);
    }

    public (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateAgentAccessToken(AgentCredential agentCredential, string sessionVersion)
    {
        var tokenJti = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, agentCredential.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, agentCredential.ClientIdentifier),
            new(JwtRegisteredClaimNames.Jti, tokenJti),
            new(ClaimTypes.NameIdentifier, agentCredential.Id.ToString()),
            new(ClaimTypes.Name, agentCredential.ClientIdentifier),
            new(ClaimTypes.Role, AuthRoles.ToName(AuthRoleType.Agent)),
            new(AuthClaimTypes.AgentId, agentCredential.AgentId.ToString()),
            new(AuthClaimTypes.Scope, "agent.api"),
            new(AuthClaimTypes.SessionVersion, sessionVersion)
        };

        return CreateToken(claims, tokenJti);
    }

    private (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateToken(IEnumerable<Claim> claims, string tokenJti)
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

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc, tokenJti);
    }
}
