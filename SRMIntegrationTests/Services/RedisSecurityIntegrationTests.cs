using Microsoft.Extensions.Options;
using SRMAuth.Configuration;
using SRMAuth.Services;
using SRMShared.Auth;
using SRMIntegrationTests.TestHelpers;
using StackExchange.Redis;

namespace SRMIntegrationTests.Services;

[NonParallelizable]
public class RedisSecurityIntegrationTests
{
    private IConnectionMultiplexer connection = null!;

    [SetUp]
    public void SetUp()
    {
        var configuration = IntegrationTestConfiguration.Build();
        var connectionString = configuration["Redis:ConnectionString"]
            ?? configuration["SRM_REDIS_CONNECTION"]
            ?? "localhost:6379,abortConnect=false";
        connection = ConnectionMultiplexer.Connect(connectionString);
    }

    [TearDown]
    public void TearDown() => connection?.Dispose();

    [Test]
    public async Task TryRotateRefreshTokenAsync_AllowsExactlyOneConcurrentSuccessor()
    {
        var store = new RedisTokenStateStore(connection);
        var userId = Guid.NewGuid();
        var principalKey = RedisTokenStateStore.BuildUserPrincipalKey(userId);
        var version = await store.RotateSessionVersionAsync(principalKey);
        var currentHash = Guid.NewGuid().ToString("N");
        await store.StoreRefreshTokenAsync(Token(userId, currentHash, version));

        var attempts = Enumerable.Range(0, 12)
            .Select(index => store.TryRotateRefreshTokenAsync(
                currentHash,
                Token(userId, $"{Guid.NewGuid():N}-{index}", version),
                DateTime.UtcNow))
            .ToArray();

        var results = await Task.WhenAll(attempts);

        Assert.That(results.Count(x => x), Is.EqualTo(1));
    }

    [Test]
    public async Task RotateSessionVersionAsync_InvalidatesPreviouslyIssuedVersion()
    {
        var store = new RedisTokenStateStore(connection);
        var principalKey = RedisTokenStateStore.BuildUserPrincipalKey(Guid.NewGuid());
        var previous = await store.GetOrCreateSessionVersionAsync(principalKey);

        var current = await store.RotateSessionVersionAsync(principalKey);

        var previousIsCurrent = await store.IsSessionVersionCurrentAsync(principalKey, previous);
        var currentIsCurrent = await store.IsSessionVersionCurrentAsync(principalKey, current);

        Assert.Multiple(() =>
        {
            Assert.That(previousIsCurrent, Is.False);
            Assert.That(currentIsCurrent, Is.True);
        });
    }

    [Test]
    public async Task RedisLoginAttemptLimiter_BlocksAndCanResetAnIdentifier()
    {
        var limiter = new RedisLoginAttemptLimiter(
            connection,
            Options.Create(new LoginSecurityOptions { MaximumFailures = 2, FailureWindowMinutes = 1 }));
        var identifier = $"test-{Guid.NewGuid():N}";

        await limiter.RecordFailureAsync("user", identifier);
        Assert.ThrowsAsync<TooManyLoginAttemptsException>(() => limiter.RecordFailureAsync("user", identifier));
        Assert.ThrowsAsync<TooManyLoginAttemptsException>(() => limiter.EnsureAllowedAsync("user", identifier));

        await limiter.ResetAsync("user", identifier);

        Assert.DoesNotThrowAsync(() => limiter.EnsureAllowedAsync("user", identifier));
    }

    [Test]
    public async Task LogoutState_RevokesRefreshAndAccessTokens()
    {
        var store = new RedisTokenStateStore(connection);
        var userId = Guid.NewGuid();
        var principalKey = RedisTokenStateStore.BuildUserPrincipalKey(userId);
        var version = await store.RotateSessionVersionAsync(principalKey);
        var refreshHash = Guid.NewGuid().ToString("N");
        var accessJti = Guid.NewGuid().ToString("N");
        await store.StoreRefreshTokenAsync(Token(userId, refreshHash, version));

        await store.RevokeRefreshTokenAsync(refreshHash, DateTime.UtcNow, null);
        await store.StoreRevokedAccessTokenAsync(userId, accessJti, DateTime.UtcNow.AddMinutes(5), "Test logout");

        var refreshState = await store.GetRefreshTokenAsync(refreshHash);
        var accessRevoked = await store.IsAccessTokenRevokedAsync(accessJti);
        Assert.Multiple(() =>
        {
            Assert.That(refreshState?.RevokedAtUtc, Is.Not.Null);
            Assert.That(accessRevoked, Is.True);
        });
    }

    private static RefreshTokenState Token(Guid userId, string hash, string version)
        => new()
        {
            UserId = userId,
            TokenHash = hash,
            SessionVersion = version,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        };
}
