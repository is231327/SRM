using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using SRMShared.DTOs.Auth;

namespace SRMApp.Services;

public class AuthApiClient(
    HttpClient httpClient,
    AuthSessionService authSessionService) : IAuthApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<HumanLoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        ConfigureBaseAddress();
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<HumanLoginResponseDto>();
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<MfaAuthenticationResponseDto?> VerifyMfaAsync(VerifyMfaRequestDto request)
    {
        ConfigureBaseAddress();
        var response = await _httpClient.PostAsJsonAsync("api/auth/mfa/verify", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MfaAuthenticationResponseDto>();
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<AuthTokenResponseDto?> RefreshAsync()
    {
        ConfigureBaseAddress();
        if (!authSessionService.CanRefresh || string.IsNullOrWhiteSpace(authSessionService.RefreshToken))
        {
            return null;
        }

        var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequestDto
        {
            RefreshToken = authSessionService.RefreshToken
        });

        if (!response.IsSuccessStatusCode)
        {
            await authSessionService.ClearAsync();
            return null;
        }

        var token = await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>();
        if (token is null)
        {
            await authSessionService.ClearAsync();
            return null;
        }

        var profile = await GetOwnProfileCoreAsync(token.AccessToken);
        if (profile is null)
        {
            await authSessionService.ClearAsync();
            return null;
        }

        await authSessionService.SetSessionAsync(token, profile);
        return token;
    }

    public async Task LogoutAsync()
    {
        ConfigureBaseAddress();
        ApplyBearerToken();

        try
        {
            await _httpClient.PostAsJsonAsync("api/auth/logout", new LogoutRequestDto
            {
                RefreshToken = authSessionService.RefreshToken ?? string.Empty
            });
        }
        catch
        {
        }

        await authSessionService.ClearAsync();
    }

    public async Task<bool> EnsureAccessTokenAsync()
    {
        if (!authSessionService.IsAuthenticated)
        {
            return false;
        }

        if (!authSessionService.IsAccessTokenExpiredOrExpiringSoon())
        {
            return true;
        }

        return await RefreshAsync() is not null;
    }

    public async Task<UserProfileDto?> GetOwnProfileAsync(string? accessToken = null)
    {
        ConfigureBaseAddress();
        if (accessToken is null)
        {
            var ensured = await EnsureAccessTokenAsync();
            if (!ensured)
            {
                return null;
            }
        }

        ApplyBearerToken(accessToken);
        return await GetOwnProfileCoreAsync(accessToken ?? authSessionService.AccessToken);
    }

    public async Task<UserProfileDto?> UpdateOwnProfileAsync(UpdateOwnProfileRequestDto request)
    {
        ConfigureBaseAddress();
        var ensured = await EnsureAccessTokenAsync();
        if (!ensured)
        {
            return null;
        }

        ApplyBearerToken();
        var response = await _httpClient.PutAsJsonAsync("api/auth/me", request);
        if (await HandleUnauthorizedAsync(response))
        {
            return null;
        }
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserProfileDto>()
            : null;
    }

    public async Task ChangePasswordAsync(ChangePasswordRequestDto request)
    {
        ConfigureBaseAddress();
        var ensured = await EnsureAccessTokenAsync();
        if (!ensured)
        {
            throw new InvalidOperationException("The session has expired.");
        }

        ApplyBearerToken();
        var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request);
        if (await HandleUnauthorizedAsync(response))
        {
            throw new InvalidOperationException("The session has expired.");
        }
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<UserProfileDto?> CreateUserAsync(CreateUserRequestDto request)
    {
        ConfigureBaseAddress();
        var ensured = await EnsureAccessTokenAsync();
        if (!ensured)
        {
            throw new InvalidOperationException("The session has expired.");
        }

        ApplyBearerToken();
        var response = await _httpClient.PostAsJsonAsync("api/auth/users", request);
        if (await HandleUnauthorizedAsync(response))
        {
            throw new InvalidOperationException("The session has expired.");
        }
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserProfileDto>();
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<List<UserManagementDto>> GetUsersAsync()
    {
        ConfigureBaseAddress();
        var ensured = await EnsureAccessTokenAsync();
        if (!ensured)
        {
            throw new InvalidOperationException("The session has expired.");
        }

        ApplyBearerToken();
        var response = await _httpClient.GetAsync("api/auth/users");
        if (await HandleUnauthorizedAsync(response))
        {
            throw new InvalidOperationException("The session has expired.");
        }
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<UserManagementDto>>() ?? [];
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<UserManagementDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto request)
    {
        ConfigureBaseAddress();
        var ensured = await EnsureAccessTokenAsync();
        if (!ensured)
        {
            throw new InvalidOperationException("The session has expired.");
        }

        ApplyBearerToken();
        var response = await _httpClient.PutAsJsonAsync($"api/auth/users/{userId}", request);
        if (await HandleUnauthorizedAsync(response))
        {
            throw new InvalidOperationException("The session has expired.");
        }
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserManagementDto>();
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<bool> ResetUserPasswordAsync(Guid userId, ResetUserPasswordRequestDto request)
    {
        ConfigureBaseAddress();
        var ensured = await EnsureAccessTokenAsync();
        if (!ensured)
        {
            throw new InvalidOperationException("The session has expired.");
        }

        ApplyBearerToken();
        var response = await _httpClient.PostAsJsonAsync($"api/auth/users/{userId}/reset-password", request);
        if (await HandleUnauthorizedAsync(response))
        {
            throw new InvalidOperationException("The session has expired.");
        }
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<bool> ResetUserMfaAsync(Guid userId)
    {
        ConfigureBaseAddress();
        var ensured = await EnsureAccessTokenAsync();
        if (!ensured)
        {
            throw new InvalidOperationException("The session has expired.");
        }

        ApplyBearerToken();
        var response = await _httpClient.PostAsync($"api/auth/users/{userId}/reset-mfa", null);
        if (await HandleUnauthorizedAsync(response))
        {
            throw new InvalidOperationException("The session has expired.");
        }
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<AgentCredentialReadDto?> CreateAgentCredentialAsync(AgentCredentialCreateRequestDto request)
    {
        ConfigureBaseAddress();
        var ensured = await EnsureAccessTokenAsync();
        if (!ensured)
        {
            throw new InvalidOperationException("The session has expired.");
        }

        ApplyBearerToken();
        var response = await _httpClient.PostAsJsonAsync("api/auth/agent-credentials", request);
        if (await HandleUnauthorizedAsync(response))
        {
            throw new InvalidOperationException("The session has expired.");
        }
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AgentCredentialReadDto>();
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<List<AgentCredentialReadDto>> GetAgentCredentialsAsync()
    {
        ConfigureBaseAddress();
        var ensured = await EnsureAccessTokenAsync();
        if (!ensured)
        {
            throw new InvalidOperationException("The session has expired.");
        }

        ApplyBearerToken();
        var response = await _httpClient.GetAsync("api/auth/agent-credentials");
        if (await HandleUnauthorizedAsync(response))
        {
            throw new InvalidOperationException("The session has expired.");
        }
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<AgentCredentialReadDto>>() ?? [];
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<AgentCredentialReadDto?> UpdateAgentCredentialAsync(Guid credentialId, AgentCredentialUpdateRequestDto request)
    {
        ConfigureBaseAddress();
        var ensured = await EnsureAccessTokenAsync();
        if (!ensured)
        {
            throw new InvalidOperationException("The session has expired.");
        }

        ApplyBearerToken();
        var response = await _httpClient.PutAsJsonAsync($"api/auth/agent-credentials/{credentialId}", request);
        if (await HandleUnauthorizedAsync(response))
        {
            throw new InvalidOperationException("The session has expired.");
        }
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AgentCredentialReadDto>();
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    private void ConfigureBaseAddress()
    {
        ArgumentNullException.ThrowIfNull(_httpClient.BaseAddress);
    }

    private void ApplyBearerToken(string? accessToken = null)
    {
        var bearerToken = accessToken ?? authSessionService.AccessToken;

        _httpClient.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken)
            ? new AuthenticationHeaderValue("Bearer", bearerToken)
            : null;
    }

    private async Task<UserProfileDto?> GetOwnProfileCoreAsync(string? accessToken)
    {
        ApplyBearerToken(accessToken);
        var response = await _httpClient.GetAsync("api/auth/me");
        if (await HandleUnauthorizedAsync(response))
        {
            return null;
        }
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserProfileDto>()
            : null;
    }

    private async Task<bool> HandleUnauthorizedAsync(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return false;
        }

        await authSessionService.ClearAsync();
        return true;
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"Request failed with status {(int)response.StatusCode}.";
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(content);
            var root = jsonDocument.RootElement;

            if (TryGetStringProperty(root, "detail", out var detail))
            {
                return detail!;
            }

            if (TryGetStringProperty(root, "title", out var title))
            {
                return title!;
            }

            if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errorsElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        var messages = property.Value
                            .EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToArray();

                        if (messages.Length > 0)
                        {
                            return string.Join(" ", messages!);
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return content;
    }

    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string? value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String)
            {
                value = property.Value.GetString();
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        value = null;
        return false;
    }
}
