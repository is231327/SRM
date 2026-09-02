namespace SRMAuth.Services.Interfaces;

public interface IMfaChallengeStore
{
    Task<(string Token, DateTime ExpiresAtUtc)> CreateAsync(MfaChallengeState state, TimeSpan lifetime, CancellationToken cancellationToken = default);
    Task<MfaChallengeState?> GetAsync(string token, CancellationToken cancellationToken = default);
    Task<MfaChallengeState?> ConsumeAsync(string token, CancellationToken cancellationToken = default);
}

public class MfaChallengeState
{
    public Guid UserId { get; set; }
    public bool IsEnrollment { get; set; }
    public string ProtectedSecret { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
}
