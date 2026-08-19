using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SRMAuth.Data;

namespace SRMIntegrationTests.TestHelpers;

internal static class AuthSqlServerDbContextFactory
{
    public static SrmAuthDbContext CreateContext()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        string connectionString = configuration.GetConnectionString("SrmAuthDatabase")
            ?? throw new InvalidOperationException("Missing connection string 'SrmAuthDatabase'.");

        var options = new DbContextOptionsBuilder<SrmAuthDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SrmAuthDbContext(options);
    }
}
