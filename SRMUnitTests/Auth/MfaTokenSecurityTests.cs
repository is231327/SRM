using System.Security.Claims;
using SRMShared.Auth;

namespace SRMUnitTests.Auth;

public class MfaTokenSecurityTests
{
    [Test]
    public void HasRequiredMfa_ShouldRejectHumanTokenWithoutMfaClaim()
    {
        var principal = Principal(new Claim(ClaimTypes.Role, AuthRoles.ToName(AuthRoleType.SystemAdmin)));
        Assert.That(MfaTokenSecurity.HasRequiredMfa(principal), Is.False);
    }

    [Test]
    public void HasRequiredMfa_ShouldAcceptHumanTokenWithMfaClaim()
    {
        var principal = Principal(
            new Claim(ClaimTypes.Role, AuthRoles.ToName(AuthRoleType.Customer)),
            new Claim(AuthClaimTypes.MfaAuthenticated, "true"));
        Assert.That(MfaTokenSecurity.HasRequiredMfa(principal), Is.True);
    }

    [Test]
    public void HasRequiredMfa_ShouldNotRequireInteractiveMfaForAgentToken()
    {
        var principal = Principal(new Claim(ClaimTypes.Role, AuthRoles.ToName(AuthRoleType.Agent)));
        Assert.That(MfaTokenSecurity.HasRequiredMfa(principal), Is.True);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "test", ClaimTypes.Name, ClaimTypes.Role));
}
