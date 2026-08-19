using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SRMAuth.Services.Interfaces;
using SRMShared.DTOs.Auth;

namespace SRMAuth.Controllers;

[ApiController]
[Route("api/auth")]
public class SRMAuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokenResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("agent/login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokenResponseDto>> AgentLogin(
        [FromBody] AgentLoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAgentAsync(request, cancellationToken);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var profile = await authService.GetOwnProfileAsync(userId.Value, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> UpdateOwnProfile(
        [FromBody] UpdateOwnProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var profile = await authService.UpdateOwnProfileAsync(userId.Value, request, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await authService.ChangePasswordAsync(userId.Value, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("users")]
    [Authorize(Roles = "SystemAdmin,Employee,CustomerAdmin")]
    public async Task<ActionResult<UserProfileDto>> CreateUser(
        [FromBody] CreateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await authService.CreateUserAsync(request, cancellationToken);
        return user is null ? Conflict() : Ok(user);
    }

    [HttpGet("users")]
    [Authorize(Roles = "SystemAdmin,Employee,CustomerAdmin")]
    public async Task<ActionResult<List<UserManagementDto>>> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await authService.GetUsersAsync(cancellationToken));
    }

    [HttpPut("users/{userId:guid}")]
    [Authorize(Roles = "SystemAdmin,Employee,CustomerAdmin")]
    public async Task<ActionResult<UserManagementDto>> UpdateUser(
        Guid userId,
        [FromBody] UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await authService.UpdateUserAsync(userId, request, cancellationToken);
        return user is null ? Conflict() : Ok(user);
    }

    [HttpPost("users/{userId:guid}/reset-password")]
    [Authorize(Roles = "SystemAdmin,Employee,CustomerAdmin")]
    public async Task<IActionResult> ResetUserPassword(
        Guid userId,
        [FromBody] ResetUserPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        return await authService.ResetUserPasswordAsync(userId, request, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpPost("agent-credentials")]
    [Authorize(Roles = "SystemAdmin,Employee")]
    public async Task<ActionResult<AgentCredentialReadDto>> CreateAgentCredential(
        [FromBody] AgentCredentialCreateRequestDto request,
        CancellationToken cancellationToken)
    {
        var credential = await authService.CreateAgentCredentialAsync(request, cancellationToken);
        return credential is null ? Conflict() : Ok(credential);
    }

    [HttpGet("agent-credentials")]
    [Authorize(Roles = "SystemAdmin,Employee")]
    public async Task<ActionResult<List<AgentCredentialReadDto>>> GetAgentCredentials(CancellationToken cancellationToken)
    {
        return Ok(await authService.GetAgentCredentialsAsync(cancellationToken));
    }

    [HttpPut("agent-credentials/{credentialId:guid}")]
    [Authorize(Roles = "SystemAdmin,Employee")]
    public async Task<ActionResult<AgentCredentialReadDto>> UpdateAgentCredential(
        Guid credentialId,
        [FromBody] AgentCredentialUpdateRequestDto request,
        CancellationToken cancellationToken)
    {
        var credential = await authService.UpdateAgentCredentialAsync(credentialId, request, cancellationToken);
        return credential is null ? Conflict() : Ok(credential);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
