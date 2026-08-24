using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SRMAuth.Configuration;
using SRMAuth.Services;
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
        context.Set<Customer>().AddRange(
            new Customer { Id = ownCustomerId, Name = "Own Customer", ContactEmail = "own@example.com", ContactPhone = "1", ExternalReference = "OWN", IsActive = true },
            new Customer { Id = otherCustomerId, Name = "Other Customer", ContactEmail = "other@example.com", ContactPhone = "2", ExternalReference = "OTHER", IsActive = true });

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

    private static AuthService CreateService(SRMAuth.Data.SrmAuthDbContext context, FakeCurrentUserContext? currentUserContext = null)
    {
        return new AuthService(
            context,
            new PasswordHasher<AuthUser>(),
            new FakeJwtTokenService(),
            currentUserContext ?? new FakeCurrentUserContext(),
            new FakeTokenStateStore(),
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
