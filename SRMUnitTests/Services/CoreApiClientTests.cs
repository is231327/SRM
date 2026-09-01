using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using SRMApp.Services;
using SRMShared.DTOs.Auth;

namespace SRMUnitTests.Services;

public class CoreApiClientTests
{
    [Test]
    public async Task UnauthorizedResponse_ClearsTheSharedBrowserSession()
    {
        var store = new InMemoryAuthSessionStore();
        var session = new AuthSessionService(store, NullLogger<AuthSessionService>.Instance);
        await session.SetSessionAsync(
            new AuthTokenResponseDto
            {
                AccessToken = "revoked-access-token",
                RefreshToken = "revoked-refresh-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
            },
            new UserProfileDto { Id = Guid.NewGuid(), Username = "user" });

        var authClient = new AuthApiClient(Client(HttpStatusCode.OK), session);
        var coreClient = new CoreApiClient(
            Client(HttpStatusCode.Unauthorized),
            session,
            authClient,
            NullLogger<CoreApiClient>.Instance);

        var customers = await coreClient.GetCustomersAsync();

        Assert.Multiple(() =>
        {
            Assert.That(customers, Is.Empty);
            Assert.That(session.IsAuthenticated, Is.False);
            Assert.That(store.State, Is.Null);
        });
    }

    private static HttpClient Client(HttpStatusCode statusCode)
        => new(new StaticResponseHandler(statusCode)) { BaseAddress = new Uri("http://localhost/") };

    private sealed class StaticResponseHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode));
    }

    private sealed class InMemoryAuthSessionStore : IAuthSessionStore
    {
        public AuthSessionState? State { get; private set; }

        public Task<AuthSessionState?> GetAsync() => Task.FromResult(State);

        public Task SetAsync(AuthSessionState state)
        {
            State = state;
            return Task.CompletedTask;
        }

        public Task DeleteAsync()
        {
            State = null;
            return Task.CompletedTask;
        }
    }
}
