using SRMShared.Auth;

namespace SRMUnitTests.TestHelpers;

public class FakeTokenStateStore : ITokenStateStore
{
    private readonly Dictionary<string, RefreshTokenState> refreshTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> revokedAccessTokens = new(StringComparer.OrdinalIgnoreCase);

    public Task StoreRefreshTokenAsync(RefreshTokenState refreshToken, CancellationToken cancellationToken = default)
    {
        refreshTokens[refreshToken.TokenHash] = refreshToken;
        return Task.CompletedTask;
    }

    public Task<RefreshTokenState?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        refreshTokens.TryGetValue(tokenHash, out var refreshToken);
        return Task.FromResult(refreshToken);
    }

    public Task RevokeRefreshTokenAsync(string tokenHash, DateTime revokedAtUtc, string? replacedByTokenHash, CancellationToken cancellationToken = default)
    {
        if (refreshTokens.TryGetValue(tokenHash, out var refreshToken))
        {
            refreshToken.RevokedAtUtc = revokedAtUtc;
            refreshToken.ReplacedByTokenHash = replacedByTokenHash;
        }

        return Task.CompletedTask;
    }

    public Task StoreRevokedAccessTokenAsync(Guid userId, string tokenJti, DateTime expiresAtUtc, string reason, CancellationToken cancellationToken = default)
    {
        revokedAccessTokens.Add(tokenJti);
        return Task.CompletedTask;
    }

    public Task<bool> IsAccessTokenRevokedAsync(string tokenJti, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(revokedAccessTokens.Contains(tokenJti));
    }
}
