using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SRMAuth.Configuration;
using SRMAuth.Services.Interfaces;
using SRMShared.Auth;
using SRMShared.Configuration;
using SRMShared.Entities;

namespace SRMAuth.Services;

public class JwtTokenService(
    IOptions<JwtOptions> options,
    IOptions<JwtCertificateOptions> signingCertificate) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JwtCertificateOptions _certificateOptions = signingCertificate.Value;

    public (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateUserAccessToken(AuthUser user, IEnumerable<string> roles, Guid? customerId)
    {
        var tokenJti = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, tokenJti),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        if (customerId.HasValue)
        {
            claims.Add(new Claim(AuthClaimTypes.CustomerId, customerId.Value.ToString()));
        }

        return CreateToken(claims, tokenJti);
    }

    public (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateAgentAccessToken(AgentCredential agentCredential)
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
            new(AuthClaimTypes.Scope, "agent.api")
        };

        return CreateToken(claims, tokenJti);
    }

    private (string AccessToken, DateTime ExpiresAtUtc, string TokenJti) CreateToken(IEnumerable<Claim> claims, string tokenJti)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var certificate = ResolveCertificate();
        var privateKey = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException(
                $"The configured certificate (thumbprint: {_certificateOptions.Thumbprint ?? _certificateOptions.Path}) does not contain a private key.");

        var signingKey = new X509SecurityKey(certificate);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc, tokenJti);
    }

    private X509Certificate2 ResolveCertificate()
    {
        // If a path is provided, load the certificate file directly.
        if (!string.IsNullOrWhiteSpace(_certificateOptions.Path))
        {
            var password = _certificateOptions.Password ?? string.Empty;
            return new X509Certificate2(_certificateOptions.Path, password);
        }

        // Otherwise, look up the certificate in the Windows certificate store.
        if (string.IsNullOrWhiteSpace(_certificateOptions.Thumbprint))
        {
            throw new InvalidOperationException(
                "Certificate thumbprint or path must be configured for JWT signing.");
        }

        var storeName = _certificateOptions.Store ?? "My";
        var storeLocation = _certificateOptions.StoreLocation ?? "CurrentUser";

        var store = new X509Store(storeName, (StoreLocation)Enum.Parse(typeof(StoreLocation), storeLocation, ignoreCase: true));
        store.Open(OpenFlags.ReadWrite);

        var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, _certificateOptions.Thumbprint, validOnly: false);
        store.Close();

        if (!certificates.Any())
        {
            throw new InvalidOperationException(
                $"No certificate found with thumbprint '{_certificateOptions.Thumbprint}' in store {storeName}\\{storeLocation}.");
        }

        return certificates[0];
    }
}
