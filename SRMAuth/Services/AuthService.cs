using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SRMAuth.Configuration;
using SRMAuth.Data;
using SRMAuth.Security;
using SRMAuth.Services.Interfaces;
using SRMShared.Auth;
using SRMShared.DTOs.Auth;
using SRMShared.Entities;
using System.Security.Cryptography;
using System.Text;

namespace SRMAuth.Services;

public class AuthService(
    SrmAuthDbContext dbContext,
    IPasswordHasher<AuthUser> passwordHasher,
    IJwtTokenService jwtTokenService,
    ICurrentUserContext currentUserContext,
    ITokenStateStore tokenStateStore,
    ILoginAttemptLimiter loginAttemptLimiter,
    ISecurityAuditService securityAuditService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    public async Task<AuthTokenResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        await loginAttemptLimiter.EnsureAllowedAsync("user", request.Username, cancellationToken);
        var user = await dbContext.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .Include(x => x.CustomerUsers)
            .FirstOrDefaultAsync(x => x.Username == request.Username && x.IsActive, cancellationToken);

        if (user is null)
        {
            await securityAuditService.RecordAsync("HumanLogin", "Failure", request.Username, description: "Invalid credentials.", cancellationToken: cancellationToken);
            await loginAttemptLimiter.RecordFailureAsync("user", request.Username, cancellationToken);
            return null;
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordResult is PasswordVerificationResult.Failed)
        {
            await securityAuditService.RecordAsync("HumanLogin", "Failure", request.Username, targetType: "AuthUser", targetId: user.Id, description: "Invalid credentials.", cancellationToken: cancellationToken);
            await loginAttemptLimiter.RecordFailureAsync("user", request.Username, cancellationToken);
            return null;
        }

        await loginAttemptLimiter.ResetAsync("user", request.Username, cancellationToken);

        user.LastLoginAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoles
            .Select(x => x.Role?.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct()
            .ToArray();

        var customerId = user.CustomerUsers.Select(x => (Guid?)x.CustomerId).FirstOrDefault();
        var sessionVersion = await tokenStateStore.GetOrCreateSessionVersionAsync(
            RedisTokenStateStore.BuildUserPrincipalKey(user.Id), cancellationToken);
        return await CreateUserAuthResponseAsync(user, roles, customerId, sessionVersion, cancellationToken);
    }

    public async Task<AuthTokenResponseDto?> LoginAgentAsync(AgentLoginRequestDto request, CancellationToken cancellationToken = default)
    {
        await loginAttemptLimiter.EnsureAllowedAsync("agent", request.ClientIdentifier, cancellationToken);
        var agentCredential = await dbContext.AgentCredentials
            .FirstOrDefaultAsync(x => x.ClientIdentifier == request.ClientIdentifier && x.IsActive, cancellationToken);

        if (agentCredential is null)
        {
            await securityAuditService.RecordAsync("AgentLogin", "Failure", request.ClientIdentifier, description: "Invalid credentials.", cancellationToken: cancellationToken);
            await loginAttemptLimiter.RecordFailureAsync("agent", request.ClientIdentifier, cancellationToken);
            return null;
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            new AuthUser { Username = agentCredential.ClientIdentifier },
            agentCredential.SecretHash,
            request.ClientSecret);

        if (verificationResult is PasswordVerificationResult.Failed)
        {
            await securityAuditService.RecordAsync("AgentLogin", "Failure", request.ClientIdentifier, targetType: "AgentCredential", targetId: agentCredential.Id, description: "Invalid credentials.", cancellationToken: cancellationToken);
            await loginAttemptLimiter.RecordFailureAsync("agent", request.ClientIdentifier, cancellationToken);
            return null;
        }

        await loginAttemptLimiter.ResetAsync("agent", request.ClientIdentifier, cancellationToken);

        agentCredential.LastAuthenticatedAtUtc = DateTime.UtcNow;
        agentCredential.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var sessionVersion = await tokenStateStore.GetOrCreateSessionVersionAsync(
            RedisTokenStateStore.BuildAgentPrincipalKey(agentCredential.Id), cancellationToken);
        var token = jwtTokenService.CreateAgentAccessToken(agentCredential, sessionVersion);
        return new AuthTokenResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            RefreshToken = string.Empty,
            Username = agentCredential.ClientIdentifier,
            Roles = new[] { "Agent" },
            AgentId = agentCredential.AgentId
        };
    }

    public async Task<AuthTokenResponseDto?> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var refreshTokenHash = HashToken(request.RefreshToken);
        var storedToken = await tokenStateStore.GetRefreshTokenAsync(refreshTokenHash, cancellationToken);

        if (storedToken is null
            || storedToken.RevokedAtUtc.HasValue
            || storedToken.ExpiresAtUtc <= DateTime.UtcNow
            || string.IsNullOrWhiteSpace(storedToken.SessionVersion)
            || !await tokenStateStore.IsSessionVersionCurrentAsync(
                RedisTokenStateStore.BuildUserPrincipalKey(storedToken.UserId),
                storedToken.SessionVersion,
                cancellationToken))
        {
            return null;
        }

        var user = await dbContext.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .Include(x => x.CustomerUsers)
            .FirstOrDefaultAsync(x => x.Id == storedToken.UserId && x.IsActive, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = user.UserRoles
            .Select(x => x.Role?.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct()
            .ToArray();
        var customerId = user.CustomerUsers.Select(x => (Guid?)x.CustomerId).FirstOrDefault();

        var (response, replacementToken) = CreateUserAuthResponse(user, roles, customerId, storedToken.SessionVersion);
        var rotated = await tokenStateStore.TryRotateRefreshTokenAsync(
            refreshTokenHash,
            replacementToken,
            DateTime.UtcNow,
            cancellationToken);
        return rotated ? response : null;
    }

    public async Task LogoutAsync(Guid userId, LogoutRequestDto request, string? currentTokenJti, DateTime? currentTokenExpiresAtUtc, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var refreshTokenHash = HashToken(request.RefreshToken);
            var refreshToken = await tokenStateStore.GetRefreshTokenAsync(refreshTokenHash, cancellationToken);
            if (refreshToken is not null && refreshToken.UserId == userId && !refreshToken.RevokedAtUtc.HasValue)
            {
                await tokenStateStore.RevokeRefreshTokenAsync(refreshTokenHash, DateTime.UtcNow, null, cancellationToken);
            }
        }

        if (!string.IsNullOrWhiteSpace(currentTokenJti) && currentTokenExpiresAtUtc.HasValue)
        {
            await tokenStateStore.StoreRevokedAccessTokenAsync(
                userId,
                currentTokenJti,
                currentTokenExpiresAtUtc.Value,
                "Logout",
                cancellationToken);
        }
    }

    public async Task<UserProfileDto?> GetOwnProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .Include(x => x.CustomerUsers)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken);

        return user is null ? null : MapProfile(user);
    }

    public async Task<UserProfileDto?> UpdateOwnProfileAsync(Guid userId, UpdateOwnProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .Include(x => x.CustomerUsers)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken);

        if (user is null)
        {
            return null;
        }

        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapProfile(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("The user account could not be found or is inactive.");
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (passwordResult is PasswordVerificationResult.Failed)
        {
            await securityAuditService.RecordAsync("PasswordChange", "Failure", string.Empty, targetType: "AuthUser", targetId: user.Id, description: "Current password verification failed.", cancellationToken: cancellationToken);
            throw new InvalidOperationException("The current password is incorrect.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new InvalidOperationException("The new password must be different from the current password.");
        }

        EnsurePasswordPolicy(request.NewPassword);

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await tokenStateStore.RotateSessionVersionAsync(
            RedisTokenStateStore.BuildUserPrincipalKey(user.Id), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await securityAuditService.RecordAsync("PasswordChange", "Success", user.Username, targetType: "AuthUser", targetId: user.Id, customerId: user.CustomerUsers.Select(x => (Guid?)x.CustomerId).FirstOrDefault(), cancellationToken: cancellationToken);
    }

    public async Task<UserProfileDto?> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();
        EnsureRequestedAssignmentAllowed(request.Roles, request.CustomerId);

        var existingUser = await dbContext.Users.AnyAsync(
            x => x.Username == request.Username || x.Email == request.Email,
            cancellationToken);

        if (existingUser)
        {
            return null;
        }

        var user = new AuthUser
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            MustChangePassword = true
        };
        EnsurePasswordPolicy(request.Password);
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var roles = await dbContext.Roles
            .Where(x => request.Roles.Contains(x.Name))
            .ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            dbContext.UserRoles.Add(new AuthUserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        var requiresCustomerAssignment = request.Roles.Contains(AuthRoles.ToName(AuthRoleType.CustomerAdmin))
            || request.Roles.Contains(AuthRoles.ToName(AuthRoleType.Customer));

        if (requiresCustomerAssignment && request.CustomerId.HasValue)
        {
            dbContext.CustomerUsers.Add(new CustomerUser
            {
                UserId = user.Id,
                CustomerId = request.CustomerId.Value
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await tokenStateStore.RotateSessionVersionAsync(
            RedisTokenStateStore.BuildUserPrincipalKey(user.Id), cancellationToken);

        user = await dbContext.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .Include(x => x.CustomerUsers)
            .FirstAsync(x => x.Id == user.Id, cancellationToken);

        await securityAuditService.RecordAsync("UserCreated", "Success", string.Empty, targetType: "AuthUser", targetId: user.Id, customerId: request.CustomerId, description: $"Created user '{user.Username}'.", cancellationToken: cancellationToken);

        return MapProfile(user);
    }

    public async Task<List<UserManagementDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();

        return await ApplyUserManagementScope(
                dbContext.Users
                    .AsNoTracking()
                    .Include(x => x.UserRoles)
                        .ThenInclude(x => x.Role)
                    .Include(x => x.CustomerUsers))
            .OrderBy(x => x.Username)
            .Select(x => MapManagementDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserManagementDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();
        EnsureRequestedAssignmentAllowed(request.Roles, request.CustomerId);

        var user = await ApplyUserManagementScope(
                dbContext.Users
                    .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                    .Include(x => x.CustomerUsers))
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var duplicateExists = await dbContext.Users.AnyAsync(
            x => x.Id != userId && (x.Username == request.Username || x.Email == request.Email),
            cancellationToken);

        if (duplicateExists)
        {
            return null;
        }

        user.Username = request.Username;
        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var roles = await dbContext.Roles
            .Where(x => request.Roles.Contains(x.Name))
            .ToListAsync(cancellationToken);

        var requestedRoleIds = roles.Select(x => x.Id).ToHashSet();
        var obsoleteRoles = user.UserRoles
            .Where(x => !requestedRoleIds.Contains(x.RoleId))
            .ToList();
        dbContext.UserRoles.RemoveRange(obsoleteRoles);

        var assignedRoleIds = user.UserRoles
            .Where(x => requestedRoleIds.Contains(x.RoleId))
            .Select(x => x.RoleId)
            .ToHashSet();
        foreach (var role in roles.Where(x => !assignedRoleIds.Contains(x.Id)))
        {
            user.UserRoles.Add(new AuthUserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        var requiresCustomerAssignment = request.Roles.Contains(AuthRoles.ToName(AuthRoleType.CustomerAdmin))
            || request.Roles.Contains(AuthRoles.ToName(AuthRoleType.Customer));

        if (requiresCustomerAssignment && request.CustomerId.HasValue)
        {
            var customerAssignment = user.CustomerUsers.SingleOrDefault();
            if (customerAssignment is null)
            {
                user.CustomerUsers.Add(new CustomerUser
                {
                    UserId = user.Id,
                    CustomerId = request.CustomerId.Value
                });
            }
            else
            {
                customerAssignment.CustomerId = request.CustomerId.Value;
                customerAssignment.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
        else
        {
            dbContext.CustomerUsers.RemoveRange(user.CustomerUsers);
        }

        await tokenStateStore.RotateSessionVersionAsync(
            RedisTokenStateStore.BuildUserPrincipalKey(user.Id), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await securityAuditService.RecordAsync("UserUpdated", "Success", string.Empty, targetType: "AuthUser", targetId: user.Id, customerId: request.CustomerId, description: $"Updated user '{user.Username}'.", cancellationToken: cancellationToken);

        user = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .Include(x => x.CustomerUsers)
            .FirstAsync(x => x.Id == userId, cancellationToken);

        return MapManagementDto(user);
    }

    public async Task<bool> ResetUserPasswordAsync(Guid userId, ResetUserPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();

        var user = await ApplyUserManagementScope(dbContext.Users)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return false;
        }

        EnsurePasswordPolicy(request.NewPassword);
        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.MustChangePassword = true;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await tokenStateStore.RotateSessionVersionAsync(
            RedisTokenStateStore.BuildUserPrincipalKey(user.Id), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await securityAuditService.RecordAsync("UserPasswordReset", "Success", string.Empty, targetType: "AuthUser", targetId: user.Id, description: $"Reset password for user '{user.Username}'.", cancellationToken: cancellationToken);
        return true;
    }

    public async Task<AgentCredentialReadDto?> CreateAgentCredentialAsync(AgentCredentialCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();

        var duplicateExists = await dbContext.AgentCredentials.AnyAsync(
            x => x.ClientIdentifier == request.ClientIdentifier,
            cancellationToken);

        if (duplicateExists)
        {
            return null;
        }

        EnsurePasswordPolicy(request.ClientSecret);

        var credential = new AgentCredential
        {
            AgentId = request.AgentId,
            ClientIdentifier = request.ClientIdentifier,
            IsActive = request.IsActive
        };
        credential.SecretHash = passwordHasher.HashPassword(new AuthUser { Username = credential.ClientIdentifier }, request.ClientSecret);

        dbContext.AgentCredentials.Add(credential);
        await tokenStateStore.RotateSessionVersionAsync(
            RedisTokenStateStore.BuildAgentPrincipalKey(credential.Id), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await securityAuditService.RecordAsync("AgentCredentialCreated", "Success", string.Empty, targetType: "AgentCredential", targetId: credential.Id, description: $"Created agent credential '{credential.ClientIdentifier}'.", cancellationToken: cancellationToken);
        return MapAgentCredential(credential);
    }

    public async Task<List<AgentCredentialReadDto>> GetAgentCredentialsAsync(CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();

        return await dbContext.AgentCredentials
            .AsNoTracking()
            .OrderBy(x => x.ClientIdentifier)
            .Select(x => MapAgentCredential(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentCredentialReadDto?> UpdateAgentCredentialAsync(Guid credentialId, AgentCredentialUpdateRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageUsers();

        var credential = await dbContext.AgentCredentials.FirstOrDefaultAsync(x => x.Id == credentialId, cancellationToken);
        if (credential is null)
        {
            return null;
        }

        var duplicateExists = await dbContext.AgentCredentials.AnyAsync(
            x => x.Id != credentialId && x.ClientIdentifier == request.ClientIdentifier,
            cancellationToken);

        if (duplicateExists)
        {
            return null;
        }

        credential.AgentId = request.AgentId;
        credential.ClientIdentifier = request.ClientIdentifier;
        credential.IsActive = request.IsActive;
        credential.UpdatedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.NewClientSecret))
        {
            EnsurePasswordPolicy(request.NewClientSecret);
            credential.SecretHash = passwordHasher.HashPassword(new AuthUser { Username = credential.ClientIdentifier }, request.NewClientSecret);
        }

        await tokenStateStore.RotateSessionVersionAsync(
            RedisTokenStateStore.BuildAgentPrincipalKey(credential.Id), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await securityAuditService.RecordAsync("AgentCredentialUpdated", "Success", string.Empty, targetType: "AgentCredential", targetId: credential.Id, description: $"Updated agent credential '{credential.ClientIdentifier}'.", cancellationToken: cancellationToken);
        return MapAgentCredential(credential);
    }

    private static UserProfileDto MapProfile(AuthUser user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Roles = user.UserRoles
                .Select(x => x.Role?.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .Distinct()
                .ToArray(),
            CustomerId = user.CustomerUsers.Select(x => (Guid?)x.CustomerId).FirstOrDefault(),
            MustChangePassword = user.MustChangePassword
        };
    }

    private static UserManagementDto MapManagementDto(AuthUser user)
    {
        return new UserManagementDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Roles = user.UserRoles
                .Select(x => x.Role?.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .Distinct()
                .ToArray(),
            CustomerId = user.CustomerUsers.Select(x => (Guid?)x.CustomerId).FirstOrDefault(),
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }

    private static AgentCredentialReadDto MapAgentCredential(AgentCredential credential)
    {
        return new AgentCredentialReadDto
        {
            Id = credential.Id,
            AgentId = credential.AgentId,
            ClientIdentifier = credential.ClientIdentifier,
            IsActive = credential.IsActive,
            LastAuthenticatedAtUtc = credential.LastAuthenticatedAtUtc,
            CreatedAtUtc = credential.CreatedAtUtc,
            UpdatedAtUtc = credential.UpdatedAtUtc
        };
    }

    private void EnsureCanManageUsers()
    {
        if (!currentUserContext.CanManageUsers)
        {
            throw new UnauthorizedAccessException("The current user is not allowed to manage users.");
        }
    }

    private IQueryable<AuthUser> ApplyUserManagementScope(IQueryable<AuthUser> query)
    {
        if (currentUserContext.IsSystemAdmin)
        {
            return query;
        }

        if (currentUserContext.IsEmployee)
        {
            return query.Where(x =>
                x.UserRoles.Any(ur =>
                    ur.Role != null &&
                    (ur.Role.Name == AuthRoles.ToName(AuthRoleType.CustomerAdmin)
                    || ur.Role.Name == AuthRoles.ToName(AuthRoleType.Customer))));
        }

        if (currentUserContext.IsCustomerAdmin)
        {
            var customerId = currentUserContext.CustomerId
                ?? throw new UnauthorizedAccessException("Customer administrators require a customer claim.");

            return query.Where(x =>
                x.CustomerUsers.Any(cu => cu.CustomerId == customerId)
                && x.UserRoles.Any(ur =>
                    ur.Role != null &&
                    (ur.Role.Name == AuthRoles.ToName(AuthRoleType.CustomerAdmin)
                    || ur.Role.Name == AuthRoles.ToName(AuthRoleType.Customer))));
        }

        return query.Where(_ => false);
    }

    private void EnsureRequestedAssignmentAllowed(
        IReadOnlyCollection<string> requestedRoles,
        Guid? requestedCustomerId)
    {
        var roles = requestedRoles
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArray();

        if (roles.Length == 0)
        {
            throw new UnauthorizedAccessException("At least one role is required.");
        }

        var humanRoleNames = AuthRoles.HumanRoles
            .Select(AuthRoles.ToName)
            .ToHashSet(StringComparer.Ordinal);
        if (roles.Any(role => !humanRoleNames.Contains(role)))
        {
            throw new UnauthorizedAccessException("One or more requested roles are invalid for a human user.");
        }

        var customerRoleNames = new[]
        {
            AuthRoles.ToName(AuthRoleType.CustomerAdmin),
            AuthRoles.ToName(AuthRoleType.Customer)
        };
        var hasCustomerRole = roles.Intersect(customerRoleNames).Any();
        var hasInternalRole = roles.Except(customerRoleNames).Any();

        if (hasCustomerRole && hasInternalRole)
        {
            throw new UnauthorizedAccessException("Internal and customer-scoped roles cannot be combined.");
        }

        if (hasCustomerRole && !requestedCustomerId.HasValue)
        {
            throw new UnauthorizedAccessException("A customer assignment is required for a customer-scoped role.");
        }

        if (!hasCustomerRole && requestedCustomerId.HasValue)
        {
            throw new UnauthorizedAccessException("A customer assignment is only valid for a customer-scoped role.");
        }

        if (currentUserContext.IsSystemAdmin)
        {
            return;
        }

        if (roles.Except(customerRoleNames).Any())
        {
            throw new UnauthorizedAccessException("The current user is not allowed to assign the requested role.");
        }

        if (currentUserContext.IsCustomerAdmin)
        {
            var currentCustomerId = currentUserContext.CustomerId
                ?? throw new UnauthorizedAccessException("Customer administrators require a customer claim.");
            var targetCustomerId = requestedCustomerId
                ?? throw new UnauthorizedAccessException("A customer assignment is required for a customer-scoped role.");

            if (targetCustomerId != currentCustomerId)
            {
                throw new UnauthorizedAccessException("Customer administrators may only manage users of their own customer.");
            }
        }
    }

    private static void EnsurePasswordPolicy(string password)
    {
        var errors = PasswordPolicy.Validate(password);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", errors));
        }
    }

    private async Task<AuthTokenResponseDto> CreateUserAuthResponseAsync(
        AuthUser user,
        IReadOnlyCollection<string> roles,
        Guid? customerId,
        string sessionVersion,
        CancellationToken cancellationToken)
    {
        var (response, refreshTokenState) = CreateUserAuthResponse(user, roles, customerId, sessionVersion);
        await tokenStateStore.StoreRefreshTokenAsync(refreshTokenState, cancellationToken);
        return response;
    }

    private (AuthTokenResponseDto Response, RefreshTokenState RefreshTokenState) CreateUserAuthResponse(
        AuthUser user,
        IReadOnlyCollection<string> roles,
        Guid? customerId,
        string sessionVersion)
    {
        var token = jwtTokenService.CreateUserAccessToken(user, roles, customerId, sessionVersion);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);
        var refreshTokenState = new RefreshTokenState
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            SessionVersion = sessionVersion,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenLifetimeDays)
        };

        return (new AuthTokenResponseDto
        {
            AccessToken = token.AccessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            Username = user.Username,
            Roles = roles,
            CustomerId = customerId
        }, refreshTokenState);
    }

    private static string GenerateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
