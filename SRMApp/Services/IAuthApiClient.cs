using SRMShared.DTOs.Auth;

namespace SRMApp.Services;

public interface IAuthApiClient
{
    Task<AuthTokenResponseDto?> LoginAsync(LoginRequestDto request);
    Task<UserProfileDto?> GetOwnProfileAsync(string? accessToken = null);
    Task<UserProfileDto?> UpdateOwnProfileAsync(UpdateOwnProfileRequestDto request);
    Task ChangePasswordAsync(ChangePasswordRequestDto request);
    Task<UserProfileDto?> CreateUserAsync(CreateUserRequestDto request);
    Task<List<UserManagementDto>> GetUsersAsync();
    Task<UserManagementDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto request);
    Task<bool> ResetUserPasswordAsync(Guid userId, ResetUserPasswordRequestDto request);
    Task<AgentCredentialReadDto?> CreateAgentCredentialAsync(AgentCredentialCreateRequestDto request);
    Task<List<AgentCredentialReadDto>> GetAgentCredentialsAsync();
    Task<AgentCredentialReadDto?> UpdateAgentCredentialAsync(Guid credentialId, AgentCredentialUpdateRequestDto request);
}
