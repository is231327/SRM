using Microsoft.Extensions.Logging.Abstractions;
using SRMApp.Services;
using SRMShared.DTOs.Auth;

namespace SRMUnitTests.Services;

public class AuthSessionServiceTests
{
    [Test]
    public async Task InitializeAsync_RestoresLoginInAnotherServiceScope()
    {
        var sharedBrowserStore = new InMemoryAuthSessionStore();
        var firstTab = CreateService(sharedBrowserStore);
        await firstTab.SetSessionAsync(
            new AuthTokenResponseDto
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
            },
            new UserProfileDto { Id = Guid.NewGuid(), Username = "admin" });

        var secondTab = CreateService(sharedBrowserStore);
        await secondTab.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(secondTab.IsAuthenticated, Is.True);
            Assert.That(secondTab.AccessToken, Is.EqualTo("access-token"));
            Assert.That(secondTab.RefreshToken, Is.EqualTo("refresh-token"));
            Assert.That(secondTab.CurrentUser?.Username, Is.EqualTo("admin"));
        });
    }

    [Test]
    public async Task ClearAsync_RemovesLoginForNewServiceScopes()
    {
        var sharedBrowserStore = new InMemoryAuthSessionStore();
        var firstTab = CreateService(sharedBrowserStore);
        await firstTab.SetSessionAsync(
            new AuthTokenResponseDto
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
            },
            new UserProfileDto { Id = Guid.NewGuid(), Username = "admin" });
        await firstTab.ClearAsync();

        var secondTab = CreateService(sharedBrowserStore);
        await secondTab.InitializeAsync();

        Assert.That(secondTab.IsAuthenticated, Is.False);
    }

    private static AuthSessionService CreateService(IAuthSessionStore store)
        => new(store, NullLogger<AuthSessionService>.Instance);

    private sealed class InMemoryAuthSessionStore : IAuthSessionStore
    {
        private AuthSessionState? _state;

        public Task<AuthSessionState?> GetAsync() => Task.FromResult(_state);

        public Task SetAsync(AuthSessionState state)
        {
            _state = state;
            return Task.CompletedTask;
        }

        public Task DeleteAsync()
        {
            _state = null;
            return Task.CompletedTask;
        }
    }
}
