using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using SRMAuth.Services.Interfaces;
using StackExchange.Redis;

namespace SRMAuth.Services;

public class RedisMfaChallengeStore(IConnectionMultiplexer connectionMultiplexer) : IMfaChallengeStore
{
    private const string KeyPrefix = "srm:auth:mfa-challenge:";
    private const string UserChallengePrefix = "srm:auth:mfa-user-challenge:";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase database = connectionMultiplexer.GetDatabase();

    public async Task<(string Token, DateTime ExpiresAtUtc)> CreateAsync(
        MfaChallengeState state,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(token);
        state.TokenHash = tokenHash;
        var expiresAtUtc = DateTime.UtcNow.Add(lifetime);
        const string script = """
            local previous = redis.call('GET', KEYS[2])
            if previous then redis.call('DEL', ARGV[1] .. previous) end
            redis.call('PSETEX', KEYS[1], ARGV[2], ARGV[3])
            redis.call('PSETEX', KEYS[2], ARGV[2], ARGV[4])
            return 1
            """;
        await database.ScriptEvaluateAsync(
            script,
            [(RedisKey)BuildKeyFromHash(tokenHash), (RedisKey)BuildUserKey(state.UserId)],
            [
                (RedisValue)KeyPrefix,
                (RedisValue)Math.Max(1L, (long)lifetime.TotalMilliseconds),
                (RedisValue)JsonSerializer.Serialize(state, SerializerOptions),
                (RedisValue)tokenHash
            ]);
        return (token, expiresAtUtc);
    }

    public async Task<MfaChallengeState?> GetAsync(string token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var value = await database.StringGetAsync(BuildKey(token));
        if (value.IsNullOrEmpty) return null;
        var state = JsonSerializer.Deserialize<MfaChallengeState>(value.ToString(), SerializerOptions);
        if (state is null) return null;
        var activeHash = await database.StringGetAsync(BuildUserKey(state.UserId));
        return !activeHash.IsNullOrEmpty && activeHash == state.TokenHash ? state : null;
    }

    public async Task<MfaChallengeState?> ConsumeAsync(string token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var state = await GetAsync(token, cancellationToken);
        if (state is null) return null;
        const string script = """
            local active = redis.call('GET', KEYS[2])
            if not active or active ~= ARGV[1] then return nil end
            local value = redis.call('GET', KEYS[1])
            if value then
                redis.call('DEL', KEYS[1])
                redis.call('DEL', KEYS[2])
            end
            return value
            """;
        var value = (RedisValue)await database.ScriptEvaluateAsync(
            script,
            [(RedisKey)BuildKey(token), (RedisKey)BuildUserKey(state.UserId)],
            [(RedisValue)state.TokenHash]);
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<MfaChallengeState>(value.ToString(), SerializerOptions);
    }

    private static string BuildKey(string token)
    {
        return BuildKeyFromHash(HashToken(token));
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string BuildKeyFromHash(string tokenHash) => $"{KeyPrefix}{tokenHash}";
    private static string BuildUserKey(Guid userId) => $"{UserChallengePrefix}{userId:N}";
}
