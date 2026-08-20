using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using SRMShared.DTOs.Auth;
using SRMShared.Auth;

namespace SRMApp.Services;

public class AuthSessionService(ProtectedSessionStorage protectedSessionStorage)
{
    private const string StorageKey = "auth-session";
    private bool _isInitialized;

    public event Action? AuthenticationChanged;

    public string? AccessToken { get; private set; }
    public UserProfileDto? CurrentUser { get; private set; }
    public bool IsInitialized => _isInitialized;

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);
    public bool IsSystemAdmin => HasRole(AuthRoleType.SystemAdmin);
    public bool IsEmployee => HasRole(AuthRoleType.Employee);
    public bool IsCustomerAdmin => HasRole(AuthRoleType.CustomerAdmin);
    public bool IsCustomer => HasRole(AuthRoleType.Customer);
    public bool IsCustomerScopedUser => IsCustomerAdmin || IsCustomer;
    public bool CanManageUsers => IsSystemAdmin || IsEmployee || IsCustomerAdmin;
    public bool CanManageCustomers => IsSystemAdmin || IsEmployee;
    public Guid? CustomerId => CurrentUser?.CustomerId;
    public bool MustChangePassword => CurrentUser?.MustChangePassword == true;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            var result = await protectedSessionStorage.GetAsync<AuthSessionState>(StorageKey);
            if (result.Success && result.Value is not null)
            {
                AccessToken = result.Value.AccessToken;
                CurrentUser = result.Value.CurrentUser;
            }
        }
        catch
        {
        }

        _isInitialized = true;
        AuthenticationChanged?.Invoke();
    }

    public async Task SetSessionAsync(string accessToken, UserProfileDto profile)
    {
        AccessToken = accessToken;
        CurrentUser = profile;
        await protectedSessionStorage.SetAsync(StorageKey, new AuthSessionState
        {
            AccessToken = accessToken,
            CurrentUser = profile
        });
        AuthenticationChanged?.Invoke();
    }

    public async Task UpdateCurrentUserAsync(UserProfileDto profile)
    {
        if (!IsAuthenticated)
        {
            return;
        }

        CurrentUser = profile;
        await PersistAsync();
        AuthenticationChanged?.Invoke();
    }

    public async Task ClearAsync()
    {
        AccessToken = null;
        CurrentUser = null;
        await protectedSessionStorage.DeleteAsync(StorageKey);
        AuthenticationChanged?.Invoke();
    }

    public void SetSessionInMemory(string accessToken, UserProfileDto profile)
    {
        AccessToken = accessToken;
        CurrentUser = profile;
        AuthenticationChanged?.Invoke();
    }

    public void ClearInMemory()
    {
        AccessToken = null;
        CurrentUser = null;
        AuthenticationChanged?.Invoke();
    }

    private async Task PersistAsync()
    {
        if (!IsAuthenticated || CurrentUser is null)
        {
            await protectedSessionStorage.DeleteAsync(StorageKey);
            return;
        }

        await protectedSessionStorage.SetAsync(StorageKey, new AuthSessionState
        {
            AccessToken = AccessToken!,
            CurrentUser = CurrentUser
        });
    }

    private bool HasRole(AuthRoleType role)
    {
        return CurrentUser?.Roles.Contains(AuthRoles.ToName(role)) == true;
    }
}

internal sealed class AuthSessionState
{
    public string AccessToken { get; set; } = string.Empty;
    public UserProfileDto? CurrentUser { get; set; }
}
