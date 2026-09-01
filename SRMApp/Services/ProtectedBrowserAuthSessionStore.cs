using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace SRMApp.Services;

public class ProtectedBrowserAuthSessionStore(
    ProtectedLocalStorage localStorage,
    ProtectedSessionStorage sessionStorage) : IAuthSessionStore
{
    private const string StorageKey = "auth-session";

    public async Task<AuthSessionState?> GetAsync()
    {
        var localResult = await localStorage.GetAsync<AuthSessionState>(StorageKey);
        if (localResult.Success && localResult.Value is not null)
        {
            return localResult.Value;
        }

        var sessionResult = await sessionStorage.GetAsync<AuthSessionState>(StorageKey);
        if (!sessionResult.Success || sessionResult.Value is null)
        {
            return null;
        }

        await localStorage.SetAsync(StorageKey, sessionResult.Value);
        await sessionStorage.DeleteAsync(StorageKey);
        return sessionResult.Value;
    }

    public async Task SetAsync(AuthSessionState state)
    {
        await localStorage.SetAsync(StorageKey, state);
        await sessionStorage.DeleteAsync(StorageKey);
    }

    public async Task DeleteAsync()
    {
        await localStorage.DeleteAsync(StorageKey);
        await sessionStorage.DeleteAsync(StorageKey);
    }
}
