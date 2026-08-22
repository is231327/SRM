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
            .AddConfiguration(IntegrationTestConfiguration.Build())
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BootstrapAdmin:Username"] = "systemadmin",
                ["BootstrapAdmin:Email"] = "systemadmin@example.local",
                ["BootstrapAdmin:Password"] = "YourTempAdminPassword123",
                ["BootstrapAdmin:FirstName"] = "System",
                ["BootstrapAdmin:LastName"] = "Administrator",
                ["BootstrapAdmin:PhoneNumber"] = string.Empty,
                ["BootstrapAdmin:MustChangePassword"] = bool.TrueString
            })
            .Build();

        AuthDbSeeder.SeedAsync(authContext, passwordHasher, configuration).GetAwaiter().GetResult();
    }
}
