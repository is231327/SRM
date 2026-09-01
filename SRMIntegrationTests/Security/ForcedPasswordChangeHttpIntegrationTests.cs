using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using SRMAuth.Middleware;
using SRMShared.Auth;

namespace SRMIntegrationTests.Security;

public class ForcedPasswordChangeHttpIntegrationTests
{
    [Test]
    public async Task Middleware_AllowsOnlyPasswordChangeLogoutAndProfileRead()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(AuthClaimTypes.MustChangePassword, "true")
            ], "test"));
            await next(context);
        });
        app.UseMiddleware<ForcedPasswordChangeMiddleware>();
        app.MapMethods("/api/auth/change-password", ["POST"], () => Results.NoContent());
        app.MapMethods("/api/auth/logout", ["POST"], () => Results.NoContent());
        app.MapMethods("/api/auth/me", ["GET", "PUT"], () => Results.Ok());
        app.MapMethods("/api/auth/users", ["GET"], () => Results.Ok());
        await app.StartAsync();

        var client = app.GetTestClient();
        var passwordChange = await client.PostAsync("/api/auth/change-password", null);
        var logout = await client.PostAsync("/api/auth/logout", null);
        var profileRead = await client.GetAsync("/api/auth/me");
        var profileWrite = await client.PutAsync("/api/auth/me", null);
        var userAdministration = await client.GetAsync("/api/auth/users");

        Assert.Multiple(() =>
        {
            Assert.That(passwordChange.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(logout.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(profileRead.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(profileWrite.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(userAdministration.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        });

        await app.DisposeAsync();
    }
}
