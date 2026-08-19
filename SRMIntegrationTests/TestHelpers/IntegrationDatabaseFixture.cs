using Microsoft.Data.SqlClient;

namespace SRMIntegrationTests.TestHelpers;

[SetUpFixture]
public class IntegrationDatabaseFixture
{
    [OneTimeSetUp]
    public void VerifyDatabaseAvailabilityAndResetSchema()
    {
        using var context = SqlServerDbContextFactory.CreateContext();

        try
        {
            if (!context.Database.CanConnect())
            {
                throw new InvalidOperationException("Cannot connect to SQL Server for integration tests.");
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("SQL Server integration test database is not reachable. Start the Docker container before running SRMIntegrationTests.", ex);
        }

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }
}
