using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SRMCore.Data;

namespace SRMIntegrationTests.TestHelpers;

internal static class SqlServerDbContextFactory
{
    public static SrmCoreDbContext CreateContext()
    {
        IConfiguration configuration = IntegrationTestConfiguration.Build();

        string connectionString = configuration.GetConnectionString("SrmCoreDatabase")
            ?? throw new InvalidOperationException("Missing connection string 'SrmCoreDatabase'.");

        var options = new DbContextOptionsBuilder<SrmCoreDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SrmCoreDbContext(options);
    }
}
