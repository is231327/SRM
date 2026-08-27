using Microsoft.Extensions.Configuration;

namespace SRMIntegrationTests.TestHelpers;

internal static class IntegrationTestConfiguration
{
    public static IConfiguration Build()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddInMemoryCollection(LoadDotEnv())
            .AddEnvironmentVariables();

        return builder.Build();
    }

    private static IDictionary<string, string?> LoadDotEnv()
    {
        var envPath = FindRepoRootEnvFile();
        if (envPath is null || !File.Exists(envPath))
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            {
                value = value[1..^1];
            }

            values[key] = value;

            if (key.Equals("SRM_TEST_SQL_AUTH_CONNECTION", StringComparison.OrdinalIgnoreCase))
            {
                values["ConnectionStrings:SrmAuthDatabase"] = value;
            }
            else if (key.Equals("SRM_TEST_SQL_CORE_CONNECTION", StringComparison.OrdinalIgnoreCase))
            {
                values["ConnectionStrings:SrmCoreDatabase"] = value;
            }
        }

        return values;
    }

    private static string? FindRepoRootEnvFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var developmentCandidate = Path.Combine(directory.FullName, "ContainerServices", ".env-development");
            if (File.Exists(developmentCandidate))
            {
                return developmentCandidate;
            }

            var localCandidate = Path.Combine(directory.FullName, "ContainerServices", ".env");
            if (File.Exists(localCandidate))
            {
                return localCandidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

