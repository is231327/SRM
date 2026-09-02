using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SRMAuth.Configuration;
using SRMAuth.Services;
using SRMAuth.Services.Interfaces;
using SRMShared.DTOs.Auth;
using SRMShared.Entities;
using SRMUnitTests.TestHelpers;
using SRMShared.Auth;

namespace SRMUnitTests.Services;

[TestFixture]
public class AuthServiceTests
{
    [Test]
    public void PasswordPolicy_ShouldRejectWeakPassword()
    {
        var errors = PasswordPolicy.Validate("short");

        Assert.That(errors, Is.Not.Empty);
        Assert.That(errors.Any(x => x.Contains("12 characters")), Is.True);
        Assert.That(errors.Any(x => x.Contains("uppercase")), Is.True);
        Assert.That(errors.Any(x => x.Contains("digit")), Is.True);
        Assert.That(errors.Any(x => x.Contains("special")), Is.True);
    }

    [Test]
    public void PasswordPolicy_ShouldAcceptStrongPassword()
    {
        var errors = PasswordPolicy.Validate("ValidPassword1!");

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void ChangePasswordAsync_ShouldThrow_WhenCurrentPasswordIsIncorrect()
    {
        using var context = AuthDbContextFactory.CreateContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var user = CreateUser(passwordHasher, "currentPassword1!");
        context.Users.Add(user);
        context.SaveChanges();

        var service = CreateService(context);

        var act = async () => await service.ChangePasswordAsync(user.Id, new ChangePasswordRequestDto
        {
            CurrentPassword = "wrongPassword1!",
            NewPassword = "NewPassword1!"
        });

        Assert.That(act, Throws.TypeOf<InvalidOperationException>()
            .With.Message.EqualTo("The current password is incorrect."));
    }

    [Test]
    public void ChangePasswordAsync_ShouldThrow_WhenNewPasswordViolatesPolicy()
    {
        using var context = AuthDbContextFactory.CreateContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var user = CreateUser(passwordHasher, "currentPassword1!");
        context.Users.Add(user);
        context.SaveChanges();

        var service = CreateService(context);

        var act = async () => await service.ChangePasswordAsync(user.Id, new ChangePasswordRequestDto
        {
            CurrentPassword = "currentPassword1!",
            NewPassword = "weak"
        });

        Assert.That(act, Throws.TypeOf<InvalidOperationException>()
            .With.Message.Contains("Password must be at least 12 characters long."));
    }

    [Test]
    public async Task ChangePasswordAsync_ShouldUpdatePassword_WhenInputIsValid()
    {
        using var context = AuthDbContextFactory.CreateContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var user = CreateUser(passwordHasher, "currentPassword1!");
        context.Users.Add(user);
        context.SaveChanges();

        var service = CreateService(context);

        await service.ChangePasswordAsync(user.Id, new ChangePasswordRequestDto
        {
            CurrentPassword = "currentPassword1!",
            NewPassword = "NewPassword1!"
        });

        var updatedUser = context.Users.Single(x => x.Id == user.Id);
        var verificationResult = passwordHasher.VerifyHashedPassword(updatedUser, updatedUser.PasswordHash, "NewPassword1!");

        Assert.That(verificationResult, Is.Not.EqualTo(PasswordVerificationResult.Failed));
        Assert.That(updatedUser.MustChangePassword, Is.False);
    }

    [Test]
    public async Task ChangePasswordAsync_ShouldInvalidateEveryPreviouslyIssuedSession()
    {
        using var context = AuthDbContextFactory.CreateContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var user = CreateUser(passwordHasher, "currentPassword1!");
        context.Users.Add(user);
        context.SaveChanges();
        var tokenStore = new FakeTokenStateStore();
        var principalKey = RedisTokenStateStore.BuildUserPrincipalKey(user.Id);
        var oldVersion = await tokenStore.GetOrCreateSessionVersionAsync(principalKey);
        var service = CreateService(context, tokenStore: tokenStore);

        await service.ChangePasswordAsync(user.Id, new ChangePasswordRequestDto
        {
            CurrentPassword = "currentPassword1!",
            NewPassword = "NewPassword1!"
        });

        Assert.That(await tokenStore.IsSessionVersionCurrentAsync(principalKey, oldVersion), Is.False);
    }

    [Test]
    public async Task RefreshAsync_ShouldAllowARefreshTokenOnlyOnce()
    {
        using var context = AuthDbContextFactory.CreateContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var user = CreateUser(passwordHasher, "currentPassword1!");
        context.Users.Add(user);
        context.SaveChanges();
        var tokenStore = new FakeTokenStateStore();
        var service = CreateService(context, tokenStore: tokenStore);
        var login = await service.LoginAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "currentPassword1!"
        });

        var firstRefresh = await service.RefreshAsync(new RefreshTokenRequestDto { RefreshToken = login!.RefreshToken });
        var replay = await service.RefreshAsync(new RefreshTokenRequestDto { RefreshToken = login.RefreshToken });

        Assert.Multiple(() =>
        {
            Assert.That(firstRefresh, Is.Not.Null);
            Assert.That(replay, Is.Null);
        });
    }

    [Test]
    public async Task LoginAsync_ShouldPersistFailureAuditWithoutSensitivePassword()
    {
        using var context = AuthDbContextFactory.CreateContext();
        var currentUser = new FakeCurrentUserContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var auditService = new SecurityAuditService(context, currentUser, httpContextAccessor);
        var service = CreateService(context, currentUser, auditService: auditService);

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "unknown-user",
            Password = "NeverStoreThis1!"
        });

        var audit = context.SecurityAuditRecords.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(audit.EventType, Is.EqualTo("HumanLogin"));
            Assert.That(audit.Outcome, Is.EqualTo("Failure"));
            Assert.That(audit.ActorIdentifier, Is.EqualTo("unknown-user"));
            Assert.That(audit.Description, Does.Not.Contain("NeverStoreThis1!"));
        });
    }

    [Test]
    public async Task GetUsersAsync_ShouldReturnOnlyCustomerScopedUsers_ForEmployee()
    {
        using var context = AuthDbContextFactory.CreateContext();
        SeedRoles(context);

        var systemAdmin = CreateUserWithoutPassword("systemadmin");
        var customerAdmin = CreateUserWithoutPassword("customeradmin");
        var customer = CreateUserWithoutPassword("customer");
        var employee = CreateUserWithoutPassword("employee");

        context.Users.AddRange(systemAdmin, customerAdmin, customer, employee);
        context.UserRoles.AddRange(
            CreateUserRole(systemAdmin.Id, GetRoleId(context, AuthRoleType.SystemAdmin)),
            CreateUserRole(customerAdmin.Id, GetRoleId(context, AuthRoleType.CustomerAdmin)),
            CreateUserRole(customer.Id, GetRoleId(context, AuthRoleType.Customer)),
            CreateUserRole(employee.Id, GetRoleId(context, AuthRoleType.Employee)));
        context.SaveChanges();

        var service = CreateService(context, new FakeCurrentUserContext
        {
            IsEmployee = true,
            CanManageUsers = true
        });

        var users = await service.GetUsersAsync();

        Assert.That(users.Select(x => x.Username), Is.EquivalentTo(new[] { "customeradmin", "customer" }));
    }

    [Test]
    public async Task GetUsersAsync_ShouldReturnOnlyOwnCustomerUsers_ForCustomerAdmin()
    {
        using var context = AuthDbContextFactory.CreateContext();
        SeedRoles(context);

        var ownCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var ownCustomerUser = CreateUserWithoutPassword("owncustomer");
        var otherCustomerUser = CreateUserWithoutPassword("othercustomer");
        context.Users.AddRange(ownCustomerUser, otherCustomerUser);
        context.UserRoles.AddRange(
            CreateUserRole(ownCustomerUser.Id, GetRoleId(context, AuthRoleType.Customer)),
            CreateUserRole(otherCustomerUser.Id, GetRoleId(context, AuthRoleType.Customer)));
        context.CustomerUsers.AddRange(
            new CustomerUser { UserId = ownCustomerUser.Id, CustomerId = ownCustomerId },
            new CustomerUser { UserId = otherCustomerUser.Id, CustomerId = otherCustomerId });
        context.SaveChanges();

        var service = CreateService(context, new FakeCurrentUserContext
        {
            IsCustomerAdmin = true,
            CanManageUsers = true,
            CustomerId = ownCustomerId
        });

        var users = await service.GetUsersAsync();

        Assert.That(users.Select(x => x.Username), Is.EquivalentTo(new[] { "owncustomer" }));
    }

    [Test]
    public async Task CreateUserAsync_ShouldAllowExternalCoreCustomerReference()
    {
        using var context = AuthDbContextFactory.CreateContext();
        SeedRoles(context);
        var customerId = Guid.NewGuid();
        var service = CreateService(context, new FakeCurrentUserContext
        {
            IsSystemAdmin = true,
            CanManageUsers = true
        });

        var created = await service.CreateUserAsync(new CreateUserRequestDto
        {
            Username = "customer-user",
            Email = "customer-user@example.com",
            FirstName = "Customer",
            LastName = "User",
            Password = "ValidPassword1!",
            Roles = [AuthRoles.ToName(AuthRoleType.Customer)],
            CustomerId = customerId
        });

        Assert.That(created, Is.Not.Null);
        Assert.That(created!.CustomerId, Is.EqualTo(customerId));
    }

    [Test]
    public async Task UpdateUserAsync_ShouldChangeCustomerWithoutRecreatingTrackedAssignments()
    {
        using var context = AuthDbContextFactory.CreateContext();
        SeedRoles(context);
        var originalCustomerId = Guid.NewGuid();
        var replacementCustomerId = Guid.NewGuid();
        var user = CreateUserWithoutPassword("moving-customer-user");
        var customerRoleId = GetRoleId(context, AuthRoleType.Customer);
        context.Users.Add(user);
        context.UserRoles.Add(CreateUserRole(user.Id, customerRoleId));
        context.CustomerUsers.Add(new CustomerUser
        {
            UserId = user.Id,
            CustomerId = originalCustomerId
        });
        context.SaveChanges();

        var service = CreateService(context, new FakeCurrentUserContext
        {
            IsSystemAdmin = true,
            CanManageUsers = true
        });

        var updated = await service.UpdateUserAsync(user.Id, new UpdateUserRequestDto
        {
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Roles = [AuthRoles.ToName(AuthRoleType.Customer)],
            CustomerId = replacementCustomerId,
            IsActive = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.Not.Null);
            Assert.That(updated!.CustomerId, Is.EqualTo(replacementCustomerId));
            Assert.That(context.CustomerUsers.Count(x => x.UserId == user.Id), Is.EqualTo(1));
            Assert.That(context.UserRoles.Count(x => x.UserId == user.Id), Is.EqualTo(1));
        });
    }

    [Test]
    public void CreateUserAsync_ShouldRejectUnknownRole()
    {
        using var context = AuthDbContextFactory.CreateContext();
        SeedRoles(context);
        var service = CreateService(context, new FakeCurrentUserContext
        {
            IsSystemAdmin = true,
            CanManageUsers = true
        });

        var action = async () => await service.CreateUserAsync(new CreateUserRequestDto
        {
            Username = "invalid-role-user",
            Email = "invalid-role-user@example.com",
            FirstName = "Invalid",
            LastName = "Role",
            Password = "ValidPassword1!",
            Roles = ["UnknownRole"]
        });

        Assert.That(action, Throws.TypeOf<UnauthorizedAccessException>());
    }

    private static AuthService CreateService(
        SRMAuth.Data.SrmAuthDbContext context,
        FakeCurrentUserContext? currentUserContext = null,
        FakeTokenStateStore? tokenStore = null,
        ISecurityAuditService? auditService = null)
    {
        return new AuthService(
            context,
            new PasswordHasher<AuthUser>(),
            new FakeJwtTokenService(),
            currentUserContext ?? new FakeCurrentUserContext(),
            tokenStore ?? new FakeTokenStateStore(),
            new NullLoginAttemptLimiter(),
            auditService ?? new NullSecurityAuditService(),
            Options.Create(new JwtOptions()));
    }

    private static AuthUser CreateUser(IPasswordHasher<AuthUser> passwordHasher, string password)
    {
        var user = CreateUserWithoutPassword("testuser");
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        return user;
    }

    private static AuthUser CreateUserWithoutPassword(string username)
    {
        return new AuthUser
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = $"{username}@example.com",
            FirstName = username,
            LastName = "User",
            PhoneNumber = "123",
            IsActive = true,
            MustChangePassword = true,
            PasswordHash = "placeholder"
        };
    }

    private static void SeedRoles(SRMAuth.Data.SrmAuthDbContext context)
    {
        context.Roles.AddRange(AuthRoles.HumanRoles.Select(role => new AuthRole
        {
            Id = Guid.NewGuid(),
            Name = AuthRoles.ToName(role),
            Description = AuthRoles.GetDescription(role)
        }));
        context.SaveChanges();
    }

    private static Guid GetRoleId(SRMAuth.Data.SrmAuthDbContext context, AuthRoleType roleType)
    {
        return context.Roles.Single(x => x.Name == AuthRoles.ToName(roleType)).Id;
    }

    private static AuthUserRole CreateUserRole(Guid userId, Guid roleId)
    {
        return new AuthUserRole
        {
            UserId = userId,
            RoleId = roleId
        };
    }
}
