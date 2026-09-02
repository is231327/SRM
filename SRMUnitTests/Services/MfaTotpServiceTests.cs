using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using SRMAuth.Configuration;
using SRMAuth.Services;

namespace SRMUnitTests.Services;

public class MfaTotpServiceTests
{
    [Test]
    public void TryVerifyTotp_ShouldValidateMicrosoftAuthenticatorCompatibleCodeAndRejectReplay()
    {
        var service = CreateService(DateTimeOffset.FromUnixTimeSeconds(59));
        const string rfcSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

        var valid = service.TryVerifyTotp(rfcSecret, "287082", null, out var timeStep);
        var replay = service.TryVerifyTotp(rfcSecret, "287082", timeStep, out _);

        Assert.Multiple(() =>
        {
            Assert.That(valid, Is.True);
            Assert.That(timeStep, Is.EqualTo(1));
            Assert.That(replay, Is.False);
        });
    }

    [Test]
    public void RecoveryCodes_ShouldBeUniqueAndNormalizeFormattingBeforeHashing()
    {
        var service = CreateService(DateTimeOffset.UtcNow);
        var codes = service.GenerateRecoveryCodes();

        Assert.Multiple(() =>
        {
            Assert.That(codes, Has.Count.EqualTo(10));
            Assert.That(codes.Distinct().Count(), Is.EqualTo(10));
            Assert.That(service.HashRecoveryCode("ABCD-EF01-2345-6789"),
                Is.EqualTo(service.HashRecoveryCode("abcdef0123456789")));
        });
    }

    [Test]
    public void ProtectSecret_ShouldRoundTripWithoutStoringPlainText()
    {
        var service = CreateService(DateTimeOffset.UtcNow);
        var secret = service.GenerateSecret();
        var protectedSecret = service.ProtectSecret(secret);

        Assert.Multiple(() =>
        {
            Assert.That(protectedSecret, Is.Not.EqualTo(secret));
            Assert.That(service.UnprotectSecret(protectedSecret), Is.EqualTo(secret));
            Assert.That(service.BuildQrCodeSvgDataUrl("admin", secret), Does.StartWith("data:image/svg+xml;base64,"));
        });
    }

    private static MfaTotpService CreateService(DateTimeOffset now)
        => new(
            new EphemeralDataProtectionProvider(),
            Options.Create(new MfaOptions { Issuer = "SRM" }),
            new FixedTimeProvider(now));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
