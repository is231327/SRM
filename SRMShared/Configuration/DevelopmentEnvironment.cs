namespace SRMShared.Configuration;

public static class DevelopmentEnvironment
{
    private const string FileName = "../ContainerServices/.env-development";

    private static readonly IReadOnlyDictionary<string, string> ConfigurationKeys =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SRM_SQL_AUTH_CONNECTION"] = "ConnectionStrings:SrmAuthDatabase",
            ["SRM_SQL_CORE_CONNECTION"] = "ConnectionStrings:SrmCoreDatabase",
            ["SRM_BOOTSTRAP_ADMIN_USERNAME"] = "BootstrapAdmin:Username",
            ["SRM_BOOTSTRAP_ADMIN_EMAIL"] = "BootstrapAdmin:Email",
            ["SRM_BOOTSTRAP_ADMIN_PASSWORD"] = "BootstrapAdmin:Password",
            ["SRM_BOOTSTRAP_ADMIN_FIRSTNAME"] = "BootstrapAdmin:FirstName",
            ["SRM_BOOTSTRAP_ADMIN_LASTNAME"] = "BootstrapAdmin:LastName",
            ["SRM_BOOTSTRAP_ADMIN_PHONENUMBER"] = "BootstrapAdmin:PhoneNumber",
            ["SRM_BOOTSTRAP_ADMIN_MUSTCHANGEPASSWORD"] = "BootstrapAdmin:MustChangePassword",
            ["SRM_JWT_ISSUER"] = "Jwt:Issuer",
            ["SRM_JWT_AUDIENCE"] = "Jwt:Audience",
            ["SRM_JWT_SIGNING_KEY"] = "Jwt:SigningKey",
            ["SRM_JWT_ACCESS_TOKEN_LIFETIME_MINUTES"] = "Jwt:AccessTokenLifetimeMinutes",
            ["SRM_AUTH_BASE_URL"] = "AgentApi:AuthBaseUrl",
            ["SRM_CORE_BASE_URL"] = "AgentApi:CoreBaseUrl",
            ["SRM_AGENT_CLIENT_IDENTIFIER"] = "AgentApi:ClientIdentifier",
            ["SRM_AGENT_CLIENT_SECRET"] = "AgentApi:ClientSecret",
            ["SRM_REDMINE_ENABLED"] = "Redmine:Enabled",
            ["SRM_REDMINE_BASE_URL"] = "Redmine:BaseUrl",
            ["SRM_REDMINE_API_KEY"] = "Redmine:ApiKey",
            ["SRM_REDMINE_PROJECT_IDENTIFIER"] = "Redmine:ProjectIdentifier",
            ["SRM_REDMINE_TRACKER_ID"] = "Redmine:TrackerId",
            ["SRM_REDMINE_STATUS_ID"] = "Redmine:StatusId",
            ["SRM_REDMINE_POLL_INTERVAL_SECONDS"] = "Redmine:PollIntervalSeconds",
            ["SRM_REDMINE_WARNING_PRIORITY_ID"] = "Redmine:WarningPriorityId",
            ["SRM_REDMINE_MAJOR_PRIORITY_ID"] = "Redmine:MajorPriorityId",
            ["SRM_REDMINE_CRITICAL_PRIORITY_ID"] = "Redmine:CriticalPriorityId",
            ["SRM_AGENT_POLLING_INTERVAL_SECONDS"] = "AgentRuntime:PollingIntervalSeconds",
            ["SRM_AGENT_CONFIGURATION_REFRESH_INTERVAL_SECONDS"] = "AgentRuntime:ConfigurationRefreshIntervalSeconds",
            ["SRM_AGENT_SHELLY_STATUS_PATH"] = "AgentRuntime:ShellyStatusPath",
            ["SRM_LOG_LEVEL_DEFAULT"] = "Logging:LogLevel:Default",
            ["SRM_LOG_LEVEL_MICROSOFT_ASPNETCORE"] = "Logging:LogLevel:Microsoft.AspNetCore"
        };

    public static IDictionary<string, string?> Load()
    {
        var path = FindFile();
        if (path is null)
        {
            throw new FileNotFoundException(
                $"Development configuration file '{FileName}' was not found. " +
                "Create it in the repository root before starting the application in Development.");
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

            AddConfigurationValue(values, key, value);
        }

        foreach (var key in ConfigurationKeys.Keys)
        {
            var environmentValue = Environment.GetEnvironmentVariable(key);
            if (environmentValue is not null)
            {
                AddConfigurationValue(values, key, environmentValue);
            }
        }

        return values;
    }

    private static void AddConfigurationValue(
        IDictionary<string, string?> values,
        string key,
        string value)
    {
        if (ConfigurationKeys.TryGetValue(key, out var configurationKey))
        {
            values[configurationKey] = value;
        }

        if (key.Equals("SRM_AUTH_BASE_URL", StringComparison.OrdinalIgnoreCase))
        {
            values["AuthApi:BaseUrl"] = value;
        }
        else if (key.Equals("SRM_CORE_BASE_URL", StringComparison.OrdinalIgnoreCase))
        {
            values["CoreApi:BaseUrl"] = value;
        }
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
            var candidate = Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
