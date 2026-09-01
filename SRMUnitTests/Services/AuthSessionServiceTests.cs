using Microsoft.Extensions.Logging.Abstractions;
using SRMApp.Services;
using SRMShared.Auth;
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

    [Test]
    public async Task SynchronizeFromStoreAsync_ClearsAnAlreadyOpenTabAfterLogout()
    {
        var sharedBrowserStore = new InMemoryAuthSessionStore();
        var firstTab = CreateService(sharedBrowserStore);
        var secondTab = CreateService(sharedBrowserStore);
        await firstTab.SetSessionAsync(Token(), Profile(AuthRoleType.SystemAdmin));
        await secondTab.InitializeAsync();

        await firstTab.ClearAsync();
        await secondTab.SynchronizeFromStoreAsync();

        Assert.Multiple(() =>
        {
            Assert.That(secondTab.IsAuthenticated, Is.False);
            Assert.That(secondTab.CurrentUser, Is.Null);
        });
    }

    [TestCase(AuthRoleType.SystemAdmin, true, true, true, true)]
    [TestCase(AuthRoleType.Employee, true, true, true, true)]
    [TestCase(AuthRoleType.CustomerAdmin, false, false, true, false)]
    [TestCase(AuthRoleType.Customer, false, false, false, false)]
    public async Task UiPageAccessPolicy_MatchesRoleMatrix(
        AuthRoleType role,
        bool customerManagement,
        bool configuration,
        bool userManagement,
        bool agentCredentials)
    {
        var service = CreateService(new InMemoryAuthSessionStore());
        await service.SetSessionAsync(Token(), Profile(role));

        Assert.Multiple(() =>
        {
            Assert.That(UiPageAccessPolicy.CanAccess("Customers", service), Is.EqualTo(customerManagement));
            Assert.That(UiPageAccessPolicy.CanAccess("CustomerDetails", service), Is.EqualTo(customerManagement));
            Assert.That(UiPageAccessPolicy.CanAccess("ServerRoomCreate", service), Is.EqualTo(configuration));
            Assert.That(UiPageAccessPolicy.CanAccess("Users", service), Is.EqualTo(userManagement));
            Assert.That(UiPageAccessPolicy.CanAccess("AgentCredentials", service), Is.EqualTo(agentCredentials));
            Assert.That(UiPageAccessPolicy.CanAccess("Dashboard", service), Is.True);
            Assert.That(UiPageAccessPolicy.CanAccess("Incidents", service), Is.True);
        });
    }

    private static AuthTokenResponseDto Token() => new()
    {
        AccessToken = "access-token",
        RefreshToken = "refresh-token",
        ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
    };

    private static UserProfileDto Profile(AuthRoleType role) => new()
    {
        Id = Guid.NewGuid(),
        Username = role.ToString(),
        Roles = [AuthRoles.ToName(role)]
    };

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
