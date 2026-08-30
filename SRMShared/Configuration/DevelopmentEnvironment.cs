namespace SRMShared.Configuration;

public static class DevelopmentEnvironment
{
    private static readonly IReadOnlyDictionary<string, string> ConfigurationKeys =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SQL_HOST"] = "SqlServer:Host", ["SQL_PORT"] = "SqlServer:Port",
            ["SQL_USERNAME"] = "SqlServer:Username", ["SQL_PASSWORD"] = "SqlServer:Password",
            ["SQL_AUTH_DATABASE"] = "SqlServer:AuthDatabase", ["SQL_CORE_DATABASE"] = "SqlServer:CoreDatabase",
            ["SQL_TEST_AUTH_DATABASE"] = "SqlServer:TestAuthDatabase",
            ["SQL_TEST_CORE_DATABASE"] = "SqlServer:TestCoreDatabase",
            ["REDIS_HOST"] = "Private:RedisHost", ["REDIS_PORT"] = "Private:RedisPort",
            ["JWT_ISSUER"] = "Jwt:Issuer", ["JWT_AUDIENCE"] = "Jwt:Audience",
            ["JWT_SIGNING_KEY"] = "Jwt:SigningKey", ["JWT_ACCESS_TOKEN_LIFETIME_MINUTES"] = "Jwt:AccessTokenLifetimeMinutes",
            ["BOOTSTRAP_ADMIN_USERNAME"] = "BootstrapAdmin:Username", ["BOOTSTRAP_ADMIN_EMAIL"] = "BootstrapAdmin:Email",
            ["BOOTSTRAP_ADMIN_PASSWORD"] = "BootstrapAdmin:Password", ["BOOTSTRAP_ADMIN_FIRST_NAME"] = "BootstrapAdmin:FirstName",
            ["BOOTSTRAP_ADMIN_LAST_NAME"] = "BootstrapAdmin:LastName", ["BOOTSTRAP_ADMIN_PHONE_NUMBER"] = "BootstrapAdmin:PhoneNumber",
            ["BOOTSTRAP_ADMIN_MUST_CHANGE_PASSWORD"] = "BootstrapAdmin:MustChangePassword",
            ["REDMINE_ENABLED"] = "Redmine:Enabled", ["REDMINE_HOST"] = "Private:RedmineHost",
            ["REDMINE_PORT"] = "Private:RedminePort",
            ["REDMINE_API_KEY"] = "Redmine:ApiKey", ["REDMINE_PROJECT_IDENTIFIER"] = "Redmine:ProjectIdentifier",
            ["REDMINE_TRACKER_ID"] = "Redmine:TrackerId", ["REDMINE_STATUS_ID"] = "Redmine:StatusId",
            ["REDMINE_POLL_INTERVAL_SECONDS"] = "Redmine:PollIntervalSeconds",
            ["REDMINE_WARNING_PRIORITY_ID"] = "Redmine:WarningPriorityId",
            ["REDMINE_MAJOR_PRIORITY_ID"] = "Redmine:MajorPriorityId",
            ["REDMINE_CRITICAL_PRIORITY_ID"] = "Redmine:CriticalPriorityId",
            ["CORE_HOST"] = "Private:CoreHost", ["CORE_PORT"] = "Private:CorePort",
            ["AUTH_HOST"] = "Private:AuthHost", ["AUTH_PORT"] = "Private:AuthPort",
            ["AGENT_CLIENT_IDENTIFIER"] = "AgentApi:ClientIdentifier", ["AGENT_CLIENT_SECRET"] = "AgentApi:ClientSecret"
        };

    private static readonly HashSet<string> InfrastructureOnlyKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "SQL_ACCEPT_EULA", "SQL_EDITION", "SQL_HOST_PORT", "REDIS_HOST_PORT",
        "REDMINE_DB_HOST", "REDMINE_DB_NAME", "REDMINE_DB_USERNAME", "REDMINE_DB_PASSWORD",
        "REDMINE_HOST_PORT", "SHELLY_PORT", "SHELLY1_HOST_PORT",
        "SHELLY2_HOST_PORT", "SHELLY3_HOST_PORT"
    };

    private static readonly string[] RelativePaths =
    [
        Path.Combine("ContainerServices", ".env.development"),
        ".env.development"
    ];

    public static IDictionary<string, string?> Load()
    {
        var path = FindFile();
        if (path is null)
        {
            throw new FileNotFoundException(
                "Development configuration file 'ContainerServices/.env.development' was not found. " +
                "Copy ContainerServices/.env.development.example to ContainerServices/.env.development and set every value.");
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                throw new FormatException($"Invalid line in '{path}': {rawLine}");
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (value.Length >= 2
                && ((value.StartsWith('"') && value.EndsWith('"'))
                    || (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            if (InfrastructureOnlyKeys.Contains(key))
            {
                continue;
            }

            if (!ConfigurationKeys.TryGetValue(key, out var configurationKey))
            {
                throw new FormatException($"Unknown key '{key}' in '{path}'.");
            }

            values[configurationKey] = value;
        }

        AddDerivedEndpoint(values, "Redis", "Redis:ConnectionString", suffix: ",abortConnect=false");
        AddDerivedEndpoint(values, "Redmine", "Redmine:BaseUrl");
        AddDerivedEndpoint(values, "Core", "CoreApi:BaseUrl");
        AddDerivedEndpoint(values, "Auth", "AuthApi:BaseUrl");
        values["AgentApi:CoreBaseUrl"] = values["CoreApi:BaseUrl"];
        values["AgentApi:AuthBaseUrl"] = values["AuthApi:BaseUrl"];

        return values;
    }

    private static void AddDerivedEndpoint(
        IDictionary<string, string?> values,
        string name,
        string targetKey,
        string suffix = "")
    {
        var hostKey = $"Private:{name}Host";
        var portKey = $"Private:{name}Port";
        if (!values.Remove(hostKey, out var host) || string.IsNullOrWhiteSpace(host)
            || !values.Remove(portKey, out var port) || string.IsNullOrWhiteSpace(port))
        {
            throw new FormatException($"{name.ToUpperInvariant()}_HOST and {name.ToUpperInvariant()}_PORT are required.");
        }

        values[targetKey] = name == "Redis"
            ? $"{host}:{port}{suffix}"
            : $"http://{host}:{port}/";
    }

    private static string? FindFile()
    {
        return FindFileFrom(Directory.GetCurrentDirectory())
            ?? FindFileFrom(AppContext.BaseDirectory);
    }

    private static string? FindFileFrom(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            foreach (var relativePath in RelativePaths)
            {
                var candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            directory = directory.Parent;
        }

        return null;
    }
}
