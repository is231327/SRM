namespace SRMApp.Services;

public interface IAuthSessionStore
{
    Task<AuthSessionState?> GetAsync();
    Task SetAsync(AuthSessionState state);
    Task DeleteAsync();
}
