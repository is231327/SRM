using System.Security.Cryptography;
using System.Text;
using SRMAuth.Services.Interfaces;

namespace SRMUnitTests.TestHelpers;

internal class FakeMfaTotpService : IMfaTotpService
{
    public string GenerateSecret() => "FAKESECRET";
    public string ProtectSecret(string secret) => $"protected:{secret}";
    public string UnprotectSecret(string protectedSecret) => protectedSecret.Replace("protected:", string.Empty, StringComparison.Ordinal);
    public string BuildQrCodeSvgDataUrl(string username, string secret) => "data:image/svg+xml;base64,PHN2Zy8+";
    public bool TryVerifyTotp(string secret, string code, long? lastUsedTimeStep, out long matchedTimeStep)
    {
        matchedTimeStep = 1;
        return code == "123456" && (!lastUsedTimeStep.HasValue || lastUsedTimeStep.Value < matchedTimeStep);
    }
    public IReadOnlyCollection<string> GenerateRecoveryCodes(int count = 10)
        => Enumerable.Range(1, count).Select(x => $"AAAA-BBBB-CCCC-{x:D4}").ToArray();
    public string HashRecoveryCode(string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Replace("-", string.Empty).ToUpperInvariant())));
}
