namespace SRMShared.Auth;

public interface ITokenStateStore
{
    Task StoreRefreshTokenAsync(RefreshTokenState refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshTokenState?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> TryRotateRefreshTokenAsync(string currentTokenHash, RefreshTokenState replacementToken, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string tokenHash, DateTime revokedAtUtc, string? replacedByTokenHash, CancellationToken cancellationToken = default);
    Task StoreRevokedAccessTokenAsync(Guid userId, string tokenJti, DateTime expiresAtUtc, string reason, CancellationToken cancellationToken = default);
    Task<bool> IsAccessTokenRevokedAsync(string tokenJti, CancellationToken cancellationToken = default);
    Task<string> GetOrCreateSessionVersionAsync(string principalKey, CancellationToken cancellationToken = default);
    Task<string> RotateSessionVersionAsync(string principalKey, CancellationToken cancellationToken = default);
    Task<bool> IsSessionVersionCurrentAsync(string principalKey, string sessionVersion, CancellationToken cancellationToken = default);
}
