using SRMShared.Auth;

namespace SRMIntegrationTests.TestHelpers;

public class FakeTokenStateStore : ITokenStateStore
{
    private readonly Dictionary<string, RefreshTokenState> refreshTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> revokedAccessTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> sessionVersions = new(StringComparer.Ordinal);

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

    public Task<bool> TryRotateRefreshTokenAsync(string currentTokenHash, RefreshTokenState replacementToken, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        if (!refreshTokens.TryGetValue(currentTokenHash, out var currentToken)
            || currentToken.RevokedAtUtc.HasValue
            || !sessionVersions.TryGetValue(RedisTokenStateStore.BuildUserPrincipalKey(currentToken.UserId), out var sessionVersion)
            || sessionVersion != currentToken.SessionVersion)
        {
            return Task.FromResult(false);
        }

        currentToken.RevokedAtUtc = revokedAtUtc;
        currentToken.ReplacedByTokenHash = replacementToken.TokenHash;
        refreshTokens[replacementToken.TokenHash] = replacementToken;
        return Task.FromResult(true);
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

    public Task<string> GetOrCreateSessionVersionAsync(string principalKey, CancellationToken cancellationToken = default)
    {
        if (!sessionVersions.TryGetValue(principalKey, out var version))
        {
            version = Guid.NewGuid().ToString("N");
            sessionVersions[principalKey] = version;
        }
        return Task.FromResult(version);
    }

    public Task<string> RotateSessionVersionAsync(string principalKey, CancellationToken cancellationToken = default)
    {
        var version = Guid.NewGuid().ToString("N");
        sessionVersions[principalKey] = version;
        return Task.FromResult(version);
    }

    public Task<bool> IsSessionVersionCurrentAsync(string principalKey, string sessionVersion, CancellationToken cancellationToken = default)
        => Task.FromResult(sessionVersions.TryGetValue(principalKey, out var current) && current == sessionVersion);
}
