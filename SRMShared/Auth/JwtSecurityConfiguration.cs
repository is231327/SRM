namespace SRMShared.Auth;

public static class JwtSecurityConfiguration
{
    public static void Validate(string? issuer, string? audience, string? signingKey)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("JWT issuer must be configured.");
        }
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("JWT audience must be configured.");
        }
        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must contain at least 32 characters.");
        }
    }
}
