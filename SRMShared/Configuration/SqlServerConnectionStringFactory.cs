using Microsoft.Extensions.Configuration;

namespace SRMShared.Configuration;

public static class SqlServerConnectionStringFactory
{
    public static string? Resolve(
        IConfiguration configuration,
        string connectionStringName,
        string? connectionStringEnvironmentKey,
        string databaseEnvironmentKey)
    {
        var explicitConnectionString = configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(connectionStringEnvironmentKey))
        {
            explicitConnectionString ??= GetValue(configuration, connectionStringEnvironmentKey);
        }

        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            return explicitConnectionString;
        }

        var host = GetValue(configuration, "SqlServer:Host", "SRM_SQL_HOST");
        var port = GetValue(configuration, "SqlServer:Port", "SRM_SQL_PORT") ?? "1433";
        var username = GetValue(configuration, "SqlServer:Username", "SRM_SQL_USERNAME");
        var password = GetValue(configuration, "SqlServer:Password", "MSSQL_SA_PASSWORD");
        var database = GetValue(configuration, GetDatabaseConfigurationKey(databaseEnvironmentKey), databaseEnvironmentKey);

        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(database))
        {
            return null;
        }

        return $"Server={host},{port};Database={database};User Id={username};Password={password};TrustServerCertificate=True;Encrypt=False";
    }

    private static string GetDatabaseConfigurationKey(string legacyKey)
    {
        return legacyKey switch
        {
            "SRM_SQL_AUTH_DATABASE" => "SqlServer:AuthDatabase",
            "SRM_SQL_CORE_DATABASE" => "SqlServer:CoreDatabase",
            "SRM_TEST_SQL_AUTH_DATABASE" => "SqlServer:TestAuthDatabase",
            "SRM_TEST_SQL_CORE_DATABASE" => "SqlServer:TestCoreDatabase",
            _ => legacyKey
        };
    }

    private static string? GetValue(IConfiguration configuration, string key, string? legacyKey = null)
    {
        return configuration[key]
            ?? Environment.GetEnvironmentVariable(key.Replace(':', '_'))
            ?? Environment.GetEnvironmentVariable(key.Replace(":", "__"))
            ?? (legacyKey is null ? null : configuration[legacyKey])
            ?? (legacyKey is null ? null : Environment.GetEnvironmentVariable(legacyKey));
    }
}
