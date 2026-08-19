using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SRMAuth.Configuration;
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
        var bootstrapOptions = Options.Create(new AuthBootstrapOptions
        {
            SystemAdminUsername = "systemadmin",
            SystemAdminPassword = "YourTempAdminPassword123",
            SystemAdminEmail = "systemadmin@example.local"
        });

        AuthDbSeeder.SeedAsync(authContext, passwordHasher, bootstrapOptions).GetAwaiter().GetResult();
    }
}
