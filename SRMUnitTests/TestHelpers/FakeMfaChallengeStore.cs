using SRMAuth.Services.Interfaces;

namespace SRMUnitTests.TestHelpers;

internal class FakeMfaChallengeStore : IMfaChallengeStore
{
    private readonly Dictionary<string, MfaChallengeState> challenges = new();
    private readonly Dictionary<Guid, string> activeChallenges = new();

    public Task<(string Token, DateTime ExpiresAtUtc)> CreateAsync(MfaChallengeState state, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        var token = Guid.NewGuid().ToString("N");
        if (activeChallenges.Remove(state.UserId, out var previousToken)) challenges.Remove(previousToken);
        state.TokenHash = token;
        challenges[token] = state;
        activeChallenges[state.UserId] = token;
        return Task.FromResult((token, DateTime.UtcNow.Add(lifetime)));
    }

    public Task<MfaChallengeState?> GetAsync(string token, CancellationToken cancellationToken = default)
        => Task.FromResult(challenges.GetValueOrDefault(token));

    public Task<MfaChallengeState?> ConsumeAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!challenges.Remove(token, out var state) || activeChallenges.GetValueOrDefault(state.UserId) != token)
            return Task.FromResult<MfaChallengeState?>(null);
        activeChallenges.Remove(state.UserId);
        return Task.FromResult<MfaChallengeState?>(state);
    }
}
