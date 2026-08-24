namespace SRMShared.Auth;

public interface ITokenStateStore
{
    Task StoreRefreshTokenAsync(RefreshTokenState refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshTokenState?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string tokenHash, DateTime revokedAtUtc, string? replacedByTokenHash, CancellationToken cancellationToken = default);
    Task StoreRevokedAccessTokenAsync(Guid userId, string tokenJti, DateTime expiresAtUtc, string reason, CancellationToken cancellationToken = default);
    Task<bool> IsAccessTokenRevokedAsync(string tokenJti, CancellationToken cancellationToken = default);
}
