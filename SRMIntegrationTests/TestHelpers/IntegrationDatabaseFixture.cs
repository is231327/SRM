using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SRMAuth.Data;
using SRMShared.Entities;

namespace SRMIntegrationTests.TestHelpers;

[SetUpFixture]
public class IntegrationDatabaseFixture
{
    [OneTimeSetUp]
    public void VerifyDatabaseAvailabilityAndResetSchema()
    {
        using var context = SqlServerDbContextFactory.CreateContext();
        using var authContext = AuthSqlServerDbContextFactory.CreateContext();

        try
        {
            if (!context.Database.CanConnect())
            {
                throw new InvalidOperationException("Cannot connect to SQL Server for integration tests.");
            }

            if (!authContext.Database.CanConnect())
            {
                throw new InvalidOperationException("Cannot connect to SQL Server for auth integration tests.");
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("SQL Server integration test database is not reachable. Start the Docker container before running SRMIntegrationTests.", ex);
        }

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        authContext.Database.EnsureDeleted();
        authContext.Database.EnsureCreated();

        var passwordHasher = new PasswordHasher<AuthUser>();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthSeedData:Users:0:Username"] = "systemadmin",
                ["AuthSeedData:Users:0:Email"] = "systemadmin@example.local",
                ["AuthSeedData:Users:0:Password"] = "YourTempAdminPassword123",
                ["AuthSeedData:Users:0:FirstName"] = "System",
                ["AuthSeedData:Users:0:LastName"] = "Administrator",
                ["AuthSeedData:Users:0:PhoneNumber"] = string.Empty,
                ["AuthSeedData:Users:0:IsActive"] = bool.TrueString,
                ["AuthSeedData:Users:0:MustChangePassword"] = bool.TrueString,
                ["AuthSeedData:Users:0:Roles:0"] = "SystemAdmin"
            })
            .Build();

        AuthDbSeeder.SeedAsync(authContext, passwordHasher, configuration).GetAwaiter().GetResult();
    }
}
