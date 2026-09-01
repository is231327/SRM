using System.Text.Json;
using StackExchange.Redis;

namespace SRMShared.Auth;

public class RedisTokenStateStore(IConnectionMultiplexer connectionMultiplexer) : ITokenStateStore
{
    private const string RefreshTokenPrefix = "srm:auth:refresh:";
    private const string RevokedTokenPrefix = "srm:auth:revoked:";
    private const string SessionVersionPrefix = "srm:auth:session-version:";

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

    public async Task<bool> TryRotateRefreshTokenAsync(
        string currentTokenHash,
        RefreshTokenState replacementToken,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentToken = await GetRefreshTokenAsync(currentTokenHash, cancellationToken);
        if (currentToken is null || currentToken.RevokedAtUtc.HasValue)
        {
            return false;
        }

        currentToken.RevokedAtUtc = revokedAtUtc;
        currentToken.ReplacedByTokenHash = replacementToken.TokenHash;

        const string script = """
            local current = redis.call('GET', KEYS[1])
            if not current then return 0 end
            local decoded = cjson.decode(current)
            if decoded['revokedAtUtc'] ~= cjson.null then return 0 end
            if decoded['sessionVersion'] ~= ARGV[1] then return 0 end
            local activeVersion = redis.call('GET', KEYS[3])
            if not activeVersion or activeVersion ~= ARGV[1] then return 0 end
            redis.call('PSETEX', KEYS[1], ARGV[2], ARGV[3])
            redis.call('PSETEX', KEYS[2], ARGV[4], ARGV[5])
            return 1
            """;

        var result = await database.ScriptEvaluateAsync(
            script,
            [
                (RedisKey)BuildRefreshTokenKey(currentTokenHash),
                (RedisKey)BuildRefreshTokenKey(replacementToken.TokenHash),
                (RedisKey)BuildSessionVersionKey(BuildUserPrincipalKey(currentToken.UserId))
            ],
            [
                (RedisValue)currentToken.SessionVersion,
                (RedisValue)Math.Max(1L, (long)CalculateTtl(currentToken.ExpiresAtUtc).TotalMilliseconds),
                (RedisValue)JsonSerializer.Serialize(currentToken, SerializerOptions),
                (RedisValue)Math.Max(1L, (long)CalculateTtl(replacementToken.ExpiresAtUtc).TotalMilliseconds),
                (RedisValue)JsonSerializer.Serialize(replacementToken, SerializerOptions)
            ]);

        return (long)result == 1;
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

    public async Task<string> GetOrCreateSessionVersionAsync(string principalKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildSessionVersionKey(principalKey);
        var proposedVersion = Guid.NewGuid().ToString("N");
        await database.StringSetAsync(key, proposedVersion, when: When.NotExists);
        var storedVersion = await database.StringGetAsync(key);
        return storedVersion.IsNullOrEmpty
            ? throw new InvalidOperationException("Could not initialize the principal session version.")
            : storedVersion.ToString();
    }

    public async Task<string> RotateSessionVersionAsync(string principalKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var version = Guid.NewGuid().ToString("N");
        await database.StringSetAsync(BuildSessionVersionKey(principalKey), version);
        return version;
    }

    public async Task<bool> IsSessionVersionCurrentAsync(string principalKey, string sessionVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sessionVersion))
        {
            return false;
        }

        var currentVersion = await database.StringGetAsync(BuildSessionVersionKey(principalKey));
        return !currentVersion.IsNullOrEmpty
            && string.Equals(currentVersion.ToString(), sessionVersion, StringComparison.Ordinal);
    }

    private static string BuildRefreshTokenKey(string tokenHash) => $"{RefreshTokenPrefix}{tokenHash}";

    private static string BuildRevokedTokenKey(string tokenJti) => $"{RevokedTokenPrefix}{tokenJti}";

    private static string BuildSessionVersionKey(string principalKey) => $"{SessionVersionPrefix}{principalKey}";

    public static string BuildUserPrincipalKey(Guid userId) => $"user:{userId:N}";

    public static string BuildAgentPrincipalKey(Guid credentialId) => $"agent:{credentialId:N}";

    private static TimeSpan CalculateTtl(DateTime expiresAtUtc)
    {
        var ttl = expiresAtUtc - DateTime.UtcNow;
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromSeconds(1);
    }
}

