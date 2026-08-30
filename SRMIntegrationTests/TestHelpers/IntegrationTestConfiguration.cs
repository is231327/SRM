using Microsoft.Extensions.Configuration;
using SRMShared.Configuration;

namespace SRMIntegrationTests.TestHelpers;

internal static class IntegrationTestConfiguration
{
    public static IConfiguration Build()
    {
        var hasExplicitConnectionStrings =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__SrmAuthDatabase"))
            && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__SrmCoreDatabase"));

        var builder = new ConfigurationBuilder();
        if (!hasExplicitConnectionStrings)
        {
            builder.AddInMemoryCollection(DevelopmentEnvironment.Load());
        }

        builder.AddEnvironmentVariables();
        var configuration = builder.Build();

        if (hasExplicitConnectionStrings)
        {
            return configuration;
        }

        var connectionStrings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:SrmAuthDatabase"] = SqlServerConnectionStringFactory.Resolve(
                configuration, "SrmAuthDatabase", null, "SRM_TEST_SQL_AUTH_DATABASE"),
            ["ConnectionStrings:SrmCoreDatabase"] = SqlServerConnectionStringFactory.Resolve(
                configuration, "SrmCoreDatabase", null, "SRM_TEST_SQL_CORE_DATABASE")
        };

        return builder.AddInMemoryCollection(connectionStrings).Build();
    }
}
