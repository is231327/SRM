using SRMShared.DTOs.Auth;

namespace SRMAuth.Services.Interfaces;

public interface IAuthService
{
    Task<AuthTokenResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponseDto?> LoginAgentAsync(AgentLoginRequestDto request, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetOwnProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> UpdateOwnProfileAsync(Guid userId, UpdateOwnProfileRequestDto request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<List<UserManagementDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserManagementDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> ResetUserPasswordAsync(Guid userId, ResetUserPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<AgentCredentialReadDto?> CreateAgentCredentialAsync(AgentCredentialCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<List<AgentCredentialReadDto>> GetAgentCredentialsAsync(CancellationToken cancellationToken = default);
    Task<AgentCredentialReadDto?> UpdateAgentCredentialAsync(Guid credentialId, AgentCredentialUpdateRequestDto request, CancellationToken cancellationToken = default);
}
