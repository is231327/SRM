using System.Security.Claims;
using SRMShared.Auth;
using SRMUnitTests.TestHelpers;

namespace SRMUnitTests.Auth;

public class TokenSessionSecurityTests
{
    [Test]
    public async Task IsCurrentAsync_RejectsSessionAfterGlobalRotation()
    {
        var userId = Guid.NewGuid();
        var store = new FakeTokenStateStore();
        var principalKey = RedisTokenStateStore.BuildUserPrincipalKey(userId);
        var version = await store.GetOrCreateSessionVersionAsync(principalKey);
        var principal = CreatePrincipal(userId, version, "Customer");

        Assert.That(await TokenSessionSecurity.IsCurrentAsync(principal, store), Is.True);

        await store.RotateSessionVersionAsync(principalKey);

        Assert.That(await TokenSessionSecurity.IsCurrentAsync(principal, store), Is.False);
    }

    [Test]
    public void MustChangePassword_ReadsSignedTokenClaim()
    {
        var principal = CreatePrincipal(Guid.NewGuid(), "version", "Customer", mustChangePassword: true);

        Assert.That(TokenSessionSecurity.MustChangePassword(principal), Is.True);
    }

    private static ClaimsPrincipal CreatePrincipal(Guid id, string version, string role, bool mustChangePassword = false)
        => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(AuthClaimTypes.SessionVersion, version),
            new Claim(AuthClaimTypes.MustChangePassword, mustChangePassword ? "true" : "false")
        ], "test"));
}
