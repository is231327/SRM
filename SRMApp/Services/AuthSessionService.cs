using Microsoft.Extensions.Logging;
using SRMShared.DTOs.Auth;
using SRMShared.Auth;

namespace SRMApp.Services;

public class AuthSessionService(
    IAuthSessionStore sessionStore,
    ILogger<AuthSessionService> logger)
{
    private bool _isInitialized;

    public event Action? AuthenticationChanged;

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? AccessTokenExpiresAtUtc { get; private set; }
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
    public bool CanManageConfiguration => IsSystemAdmin || IsEmployee;
    public bool CanManageAgentCredentials => IsSystemAdmin || IsEmployee;
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
            var state = await sessionStore.GetAsync();
            if (state is not null)
            {
                AccessToken = state.AccessToken;
                RefreshToken = state.RefreshToken;
                AccessTokenExpiresAtUtc = state.AccessTokenExpiresAtUtc;
                CurrentUser = state.CurrentUser;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to restore the protected browser auth session.");
        }

        _isInitialized = true;
        AuthenticationChanged?.Invoke();
    }

    public async Task SetSessionAsync(AuthTokenResponseDto tokenResponse, UserProfileDto profile)
    {
        AccessToken = tokenResponse.AccessToken;
        RefreshToken = tokenResponse.RefreshToken;
        AccessTokenExpiresAtUtc = tokenResponse.ExpiresAtUtc;
        CurrentUser = profile;

        try
        {
            await sessionStore.SetAsync(new AuthSessionState
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                AccessTokenExpiresAtUtc = tokenResponse.ExpiresAtUtc,
                CurrentUser = profile
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to persist the protected browser auth session. Continuing with in-memory session state only.");
        }

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
        RefreshToken = null;
        AccessTokenExpiresAtUtc = null;
        CurrentUser = null;

        try
        {
            await sessionStore.DeleteAsync();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to clear the protected browser auth session.");
        }

        AuthenticationChanged?.Invoke();
    }

    public bool CanRefresh => !string.IsNullOrWhiteSpace(RefreshToken);

    public bool IsAccessTokenExpiredOrExpiringSoon()
    {
        return !AccessTokenExpiresAtUtc.HasValue || AccessTokenExpiresAtUtc.Value <= DateTime.UtcNow.AddMinutes(1);
    }

    public void SetSessionInMemory(AuthTokenResponseDto tokenResponse, UserProfileDto profile)
    {
        AccessToken = tokenResponse.AccessToken;
        RefreshToken = tokenResponse.RefreshToken;
        AccessTokenExpiresAtUtc = tokenResponse.ExpiresAtUtc;
        CurrentUser = profile;
        AuthenticationChanged?.Invoke();
    }

    public void ClearInMemory()
    {
        AccessToken = null;
        RefreshToken = null;
        AccessTokenExpiresAtUtc = null;
        CurrentUser = null;
        AuthenticationChanged?.Invoke();
    }

    private async Task PersistAsync()
    {
        try
        {
            if (!IsAuthenticated || CurrentUser is null)
            {
                await sessionStore.DeleteAsync();
                return;
            }

            await sessionStore.SetAsync(new AuthSessionState
            {
                AccessToken = AccessToken!,
                RefreshToken = RefreshToken ?? string.Empty,
                AccessTokenExpiresAtUtc = AccessTokenExpiresAtUtc,
                CurrentUser = CurrentUser
            });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to persist the protected browser auth session update.");
        }
    }

    private bool HasRole(AuthRoleType role)
    {
        return CurrentUser?.Roles.Contains(AuthRoles.ToName(role)) == true;
    }
}

public sealed class AuthSessionState
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime? AccessTokenExpiresAtUtc { get; set; }
    public UserProfileDto? CurrentUser { get; set; }
}
