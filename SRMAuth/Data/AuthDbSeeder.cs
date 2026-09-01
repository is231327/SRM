using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SRMShared.Auth;
using SRMShared.Entities;

namespace SRMAuth.Data;

public static class AuthDbSeeder
{
    public static async Task SeedAsync(
        SrmAuthDbContext dbContext,
        IPasswordHasher<AuthUser> passwordHasher,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        foreach (var roleType in AuthRoles.All)
        {
            var roleName = AuthRoles.ToName(roleType);
            var existingRole = await dbContext.Roles.FirstOrDefaultAsync(x => x.Name == roleName, cancellationToken);

            if (existingRole is null)
            {
                dbContext.Roles.Add(new AuthRole
                {
                    Name = roleName,
                    Description = AuthRoles.GetDescription(roleType),
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
            else if (existingRole.Description != AuthRoles.GetDescription(roleType))
            {
                existingRole.Description = AuthRoles.GetDescription(roleType);
                existingRole.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var bootstrapAdmin = BuildBootstrapAdmin(configuration);
        if (bootstrapAdmin == null)
        {
            throw new InvalidOperationException("SRMAuth-ERROR: BootstrapAdmin configuration is missing or invalid. Please provide valid BootstrapAdmin settings in the configuration.");
        }
        List<AuthSeedUser> seedUsers = bootstrapAdmin is null
            ? new List<AuthSeedUser>()
            : new List<AuthSeedUser> { bootstrapAdmin };
        if (seedUsers.Count == 0)
        {
            return;
        }

        foreach (var seedUser in seedUsers.Where(x =>
                     !string.IsNullOrWhiteSpace(x.Username)
                     && !string.IsNullOrWhiteSpace(x.Email)
                     && !string.IsNullOrWhiteSpace(x.Password)))
        {
            var user = await dbContext.Users
                .Include(x => x.UserRoles)
                .FirstOrDefaultAsync(x => x.Username == seedUser.Username, cancellationToken);

            if (user is null)
            {
                user = new AuthUser
                {
                    Username = seedUser.Username,
                    Email = seedUser.Email,
                    FirstName = seedUser.FirstName,
                    LastName = seedUser.LastName,
                    PhoneNumber = seedUser.PhoneNumber,
                    IsActive = seedUser.IsActive,
                    MustChangePassword = seedUser.MustChangePassword
                };
                user.PasswordHash = passwordHasher.HashPassword(user, seedUser.Password);
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var roleName in seedUser.Roles
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Name == roleName, cancellationToken);
                if (role is null)
                {
                    continue;
                }

                var hasRole = await dbContext.UserRoles.AnyAsync(
                    x => x.UserId == user.Id && x.RoleId == role.Id,
                    cancellationToken);

                if (!hasRole)
                {
                    dbContext.UserRoles.Add(new AuthUserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    });
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AuthSeedUser? BuildBootstrapAdmin(IConfiguration configuration)
    {
        var username = configuration["BootstrapAdmin:Username"];
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        return new AuthSeedUser
        {
            Username = username,
            Email = email,
            Password = password,
            FirstName = configuration["BootstrapAdmin:FirstName"] ?? string.Empty,
            LastName = configuration["BootstrapAdmin:LastName"] ?? string.Empty,
            PhoneNumber = configuration["BootstrapAdmin:PhoneNumber"] ?? string.Empty,
            IsActive = true,
            MustChangePassword = bool.TryParse(configuration["BootstrapAdmin:MustChangePassword"], out var mustChangePassword)
                ? mustChangePassword
                : false,
            Roles = ["SystemAdmin"]
        };
    }
}

internal sealed class AuthSeedUser
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public List<string> Roles { get; set; } = [];
}
