using System.Text.Json;
using StackExchange.Redis;

namespace SRMShared.Auth;

public class RedisTokenStateStore(IConnectionMultiplexer connectionMultiplexer) : ITokenStateStore
{
    private const string RefreshTokenPrefix = "srm:auth:refresh:";
    private const string RevokedTokenPrefix = "srm:auth:revoked:";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase database = connectionMultiplexer.GetDatabase();

    public async Task StoreRefreshTokenAsync(RefreshTokenState refreshToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(refreshToken, SerializerOptions);
        await database.StringSetAsync(BuildRefreshTokenKey(refreshToken.TokenHash), payload, CalculateTtl(refreshToken.ExpiresAtUtc));
    }

    public async Task<RefreshTokenState?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = await database.StringGetAsync(BuildRefreshTokenKey(tokenHash));
        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<RefreshTokenState>(value.ToString(), SerializerOptions);
    }

    public async Task RevokeRefreshTokenAsync(string tokenHash, DateTime revokedAtUtc, string? replacedByTokenHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var refreshToken = await GetRefreshTokenAsync(tokenHash, cancellationToken);
        if (refreshToken is null)
        {
            return;
        }

        refreshToken.RevokedAtUtc = revokedAtUtc;
        refreshToken.ReplacedByTokenHash = string.IsNullOrWhiteSpace(replacedByTokenHash) ? null : replacedByTokenHash;

        var payload = JsonSerializer.Serialize(refreshToken, SerializerOptions);
        await database.StringSetAsync(BuildRefreshTokenKey(tokenHash), payload, CalculateTtl(refreshToken.ExpiresAtUtc));
    }

    public async Task StoreRevokedAccessTokenAsync(Guid userId, string tokenJti, DateTime expiresAtUtc, string reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(new RevokedAccessTokenState
        {
            UserId = userId,
            TokenJti = tokenJti,
            ExpiresAtUtc = expiresAtUtc,
            Reason = reason,
            CreatedAtUtc = DateTime.UtcNow
        }, SerializerOptions);

        await database.StringSetAsync(BuildRevokedTokenKey(tokenJti), payload, CalculateTtl(expiresAtUtc));
    }

    public Task<bool> IsAccessTokenRevokedAsync(string tokenJti, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return database.KeyExistsAsync(BuildRevokedTokenKey(tokenJti));
    }

    private static string BuildRefreshTokenKey(string tokenHash) => $"{RefreshTokenPrefix}{tokenHash}";

    private static string BuildRevokedTokenKey(string tokenJti) => $"{RevokedTokenPrefix}{tokenJti}";

    private static TimeSpan CalculateTtl(DateTime expiresAtUtc)
    {
        var ttl = expiresAtUtc - DateTime.UtcNow;
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromSeconds(1);
    }
}

