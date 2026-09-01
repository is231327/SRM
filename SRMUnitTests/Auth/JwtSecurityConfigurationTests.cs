using SRMShared.Auth;

namespace SRMUnitTests.Auth;

public class JwtSecurityConfigurationTests
{
    [TestCase(null, "audience")]
    [TestCase("issuer", null)]
    public void Validate_RejectsMissingIssuerOrAudience(string? issuer, string? audience)
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtSecurityConfiguration.Validate(issuer, audience, new string('x', 32)));
    }

    [Test]
    public void Validate_RejectsShortSigningKeys()
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtSecurityConfiguration.Validate("issuer", "audience", "too-short"));
    }

    [Test]
    public void Validate_AcceptsCompleteStrongConfiguration()
    {
        Assert.DoesNotThrow(() =>
            JwtSecurityConfiguration.Validate("issuer", "audience", new string('x', 32)));
    }
}
