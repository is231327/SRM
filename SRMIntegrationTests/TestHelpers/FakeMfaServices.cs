using SRMAuth.Services.Interfaces;

namespace SRMIntegrationTests.TestHelpers;

internal class FakeMfaChallengeStore : IMfaChallengeStore
{
    public Task<(string Token, DateTime ExpiresAtUtc)> CreateAsync(MfaChallengeState state, TimeSpan lifetime, CancellationToken cancellationToken = default)
        => Task.FromResult(("unused", DateTime.UtcNow.Add(lifetime)));
    public Task<MfaChallengeState?> GetAsync(string token, CancellationToken cancellationToken = default) => Task.FromResult<MfaChallengeState?>(null);
    public Task<MfaChallengeState?> ConsumeAsync(string token, CancellationToken cancellationToken = default) => Task.FromResult<MfaChallengeState?>(null);
}

internal class FakeMfaTotpService : IMfaTotpService
{
    public string GenerateSecret() => "unused";
    public string ProtectSecret(string secret) => secret;
    public string UnprotectSecret(string protectedSecret) => protectedSecret;
    public string BuildQrCodeSvgDataUrl(string username, string secret) => string.Empty;
    public bool TryVerifyTotp(string secret, string code, long? lastUsedTimeStep, out long matchedTimeStep) { matchedTimeStep = 0; return false; }
    public IReadOnlyCollection<string> GenerateRecoveryCodes(int count = 10) => [];
    public string HashRecoveryCode(string code) => string.Empty;
}
