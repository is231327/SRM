using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SRMAuth.Configuration;
using SRMAuth.Services.Interfaces;
using StackExchange.Redis;

namespace SRMAuth.Services;

public sealed class RedisLoginAttemptLimiter : ILoginAttemptLimiter
{
    private const string KeyPrefix = "srm:auth:login-failures:";
    private readonly IDatabase database;
    private readonly int maximumFailures;
    private readonly TimeSpan failureWindow;

    public RedisLoginAttemptLimiter(IConnectionMultiplexer connectionMultiplexer, IOptions<LoginSecurityOptions> options)
    {
        database = connectionMultiplexer.GetDatabase();
        maximumFailures = Math.Max(1, options.Value.MaximumFailures);
        failureWindow = TimeSpan.FromMinutes(Math.Max(1, options.Value.FailureWindowMinutes));
    }

    public async Task EnsureAllowedAsync(string principalType, string identifier, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildKey(principalType, identifier);
        var failures = await database.StringGetAsync(key);
        if (failures.TryParse(out long count) && count >= maximumFailures)
        {
            throw new TooManyLoginAttemptsException(await GetRetryAfterAsync(key));
        }
    }

    public async Task RecordFailureAsync(string principalType, string identifier, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildKey(principalType, identifier);
        const string script = """
            local count = redis.call('INCR', KEYS[1])
            if count == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end
            return count
            """;
        var result = await database.ScriptEvaluateAsync(
            script,
            [(RedisKey)key],
            [(RedisValue)Math.Max(1L, (long)failureWindow.TotalMilliseconds)]);

        if ((long)result >= maximumFailures)
        {
            throw new TooManyLoginAttemptsException(await GetRetryAfterAsync(key));
        }
    }

    public Task ResetAsync(string principalType, string identifier, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return database.KeyDeleteAsync(BuildKey(principalType, identifier));
    }

    private async Task<TimeSpan> GetRetryAfterAsync(string key)
        => await database.KeyTimeToLiveAsync(key) ?? failureWindow;

    private static string BuildKey(string principalType, string identifier)
    {
        var normalized = $"{principalType.Trim().ToLowerInvariant()}:{identifier.Trim().ToUpperInvariant()}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return $"{KeyPrefix}{hash}";
    }
}
