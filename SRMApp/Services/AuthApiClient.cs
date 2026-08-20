using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SRMShared.DTOs.Auth;

namespace SRMApp.Services;

public class AuthApiClient(
    HttpClient httpClient,
    AuthSessionService authSessionService) : IAuthApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<AuthTokenResponseDto?> LoginAsync(LoginRequestDto request)
    {
        ConfigureBaseAddress();
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>()
            : null;
    }

    public async Task<UserProfileDto?> GetOwnProfileAsync(string? accessToken = null)
    {
        ConfigureBaseAddress();
        ApplyBearerToken(accessToken);
        var response = await _httpClient.GetAsync("api/auth/me");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserProfileDto>()
            : null;
    }

    public async Task<UserProfileDto?> UpdateOwnProfileAsync(UpdateOwnProfileRequestDto request)
    {
        ConfigureBaseAddress();
        ApplyBearerToken();
        var response = await _httpClient.PutAsJsonAsync("api/auth/me", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserProfileDto>()
            : null;
    }

    public async Task ChangePasswordAsync(ChangePasswordRequestDto request)
    {
        ConfigureBaseAddress();
        ApplyBearerToken();
        var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<UserProfileDto?> CreateUserAsync(CreateUserRequestDto request)
    {
        ConfigureBaseAddress();
        ApplyBearerToken();
        var response = await _httpClient.PostAsJsonAsync("api/auth/users", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserProfileDto>();
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<List<UserManagementDto>> GetUsersAsync()
    {
        ConfigureBaseAddress();
        ApplyBearerToken();
        var response = await _httpClient.GetAsync("api/auth/users");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<UserManagementDto>>() ?? [];
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<UserManagementDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto request)
    {
        ConfigureBaseAddress();
        ApplyBearerToken();
        var response = await _httpClient.PutAsJsonAsync($"api/auth/users/{userId}", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserManagementDto>();
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<bool> ResetUserPasswordAsync(Guid userId, ResetUserPasswordRequestDto request)
    {
        ConfigureBaseAddress();
        ApplyBearerToken();
        var response = await _httpClient.PostAsJsonAsync($"api/auth/users/{userId}/reset-password", request);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<AgentCredentialReadDto?> CreateAgentCredentialAsync(AgentCredentialCreateRequestDto request)
    {
        ConfigureBaseAddress();
        ApplyBearerToken();
        var response = await _httpClient.PostAsJsonAsync("api/auth/agent-credentials", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AgentCredentialReadDto>();
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<List<AgentCredentialReadDto>> GetAgentCredentialsAsync()
    {
        ConfigureBaseAddress();
        ApplyBearerToken();
        var response = await _httpClient.GetAsync("api/auth/agent-credentials");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<AgentCredentialReadDto>>() ?? [];
        }

        throw new InvalidOperationException(await ExtractErrorMessageAsync(response));
    }

    public async Task<AgentCredentialReadDto?> UpdateAgentCredentialAsync(Guid credentialId, AgentCredentialUpdateRequestDto request)
    {
        ConfigureBaseAddress();
        ApplyBearerToken();
        var response = await _httpClient.PutAsJsonAsync($"api/auth/agent-credentials/{credentialId}", request);
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
