namespace SRMAuth.Services.Interfaces;

public interface IMfaTotpService
{
    string GenerateSecret();
    string ProtectSecret(string secret);
    string UnprotectSecret(string protectedSecret);
    string BuildQrCodeSvgDataUrl(string username, string secret);
    bool TryVerifyTotp(string secret, string code, long? lastUsedTimeStep, out long matchedTimeStep);
    IReadOnlyCollection<string> GenerateRecoveryCodes(int count = 10);
    string HashRecoveryCode(string code);
}
