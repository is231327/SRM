using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using SRMShared.Entities;

const string agentId = "10000000-0000-0000-0000-000000000021";

var host = Required("SQL_HOST");
var port = Required("SQL_PORT");
var username = Required("SQL_USERNAME");
var sqlPassword = Required("SQL_PASSWORD");
var coreDatabase = Required("SQL_CORE_DATABASE");
var authDatabase = Required("SQL_AUTH_DATABASE");
var agentClientIdentifier = Required("AGENT_CLIENT_IDENTIFIER");
var agentSecret = Required("AGENT_CLIENT_SECRET");
var shellyBaseUrl = Required("SHELLY_BASE_URL");
var now = DateTime.UtcNow;

var coreConnectionString = BuildConnectionString(coreDatabase);
var authConnectionString = BuildConnectionString(authDatabase);

await using (var connection = new SqlConnection(coreConnectionString))
{
    await connection.OpenAsync();
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = GetCoreSeedSql();
    command.Parameters.AddWithValue("@now", now);
    command.Parameters.AddWithValue("@shellyBaseUrl", shellyBaseUrl);
    command.Parameters.AddWithValue("@agentClientIdentifier", agentClientIdentifier);
    await command.ExecuteNonQueryAsync();
    await transaction.CommitAsync();
}

var credential = new AgentCredential { AgentId = Guid.Parse(agentId), ClientIdentifier = agentClientIdentifier };
var secretHash = new PasswordHasher<AgentCredential>().HashPassword(credential, agentSecret);

await using (var connection = new SqlConnection(authConnectionString))
{
    await connection.OpenAsync();
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = GetAuthSeedSql();
    command.Parameters.AddWithValue("@now", now);
    command.Parameters.AddWithValue("@secretHash", secretHash);
    command.Parameters.AddWithValue("@agentClientIdentifier", agentClientIdentifier);
    await command.ExecuteNonQueryAsync();
    await transaction.CommitAsync();
}

await using (var connection = new SqlConnection(coreConnectionString))
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT
          (SELECT COUNT(*) FROM Customers WHERE ExternalReference = 'DEMO-NORTHSTAR-001') AS DemoCustomers,
          (SELECT COUNT(*) FROM ServerRooms WHERE CustomerId = '10000000-0000-0000-0000-000000000001') AS DemoRooms,
          (SELECT COUNT(*) FROM Agents WHERE Id = '10000000-0000-0000-0000-000000000021') AS DemoAgents,
          (SELECT COUNT(*) FROM ShellyDevices WHERE AgentId = '10000000-0000-0000-0000-000000000021') AS DemoShellyDevices,
          (SELECT COUNT(*) FROM MonitoredDevices WHERE AgentId = '10000000-0000-0000-0000-000000000021') AS DemoMonitoredDevices,
          (SELECT COUNT(*) FROM SensorReadings WHERE ShellyDeviceId = '10000000-0000-0000-0000-000000000031') AS DemoReadings,
          (SELECT COUNT(*) FROM Incidents WHERE ServerRoomId = '10000000-0000-0000-0000-000000000011') AS DemoIncidents;
        """;
    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();
    Console.WriteLine(
        "Demo seed complete: customers={0}, rooms={1}, agents={2}, Shelly devices={3}, monitored devices={4}, readings={5}, incidents={6}.",
        reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6));
}

string BuildConnectionString(string database) => new SqlConnectionStringBuilder
{
    DataSource = $"{host},{port}",
    InitialCatalog = database,
    UserID = username,
    Password = sqlPassword,
    Encrypt = true,
    TrustServerCertificate = true,
    ConnectTimeout = 30
}.ConnectionString;

static string Required(string name) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Missing required environment variable '{name}'.");

static string GetCoreSeedSql() => """
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM Customers WHERE Id = '10000000-0000-0000-0000-000000000001')
  INSERT Customers (Id, ExternalReference, Name, ContactEmail, ContactPhone, IsActive, CreatedAtUtc, UpdatedAtUtc)
  VALUES ('10000000-0000-0000-0000-000000000001', 'DEMO-NORTHSTAR-001', 'Northstar Cloud Services', 'operations@northstar-demo.example', '+49 30 5550 1000', 1, @now, @now);

IF NOT EXISTS (SELECT 1 FROM ServerRooms WHERE Id = '10000000-0000-0000-0000-000000000011')
  INSERT ServerRooms (Id, CustomerId, Name, LocationDescription, TemperatureWarningThreshold, TemperatureCriticalThreshold, MonitoringEnabled, CreatedAtUtc, UpdatedAtUtc)
  VALUES ('10000000-0000-0000-0000-000000000011', '10000000-0000-0000-0000-000000000001', 'Berlin Primary DC', 'Berlin, Building A, Level 2, Room DC-201', 26, 31, 1, @now, @now);

IF NOT EXISTS (SELECT 1 FROM ServerRooms WHERE Id = '10000000-0000-0000-0000-000000000012')
  INSERT ServerRooms (Id, CustomerId, Name, LocationDescription, TemperatureWarningThreshold, TemperatureCriticalThreshold, MonitoringEnabled, CreatedAtUtc, UpdatedAtUtc)
  VALUES ('10000000-0000-0000-0000-000000000012', '10000000-0000-0000-0000-000000000001', 'Munich Recovery Site', 'Munich, Building C, Basement, Room DR-01', 25, 30, 1, @now, @now);

IF NOT EXISTS (SELECT 1 FROM Agents WHERE Id = '10000000-0000-0000-0000-000000000021')
  INSERT Agents (Id, ServerRoomId, Name, ApiKeyReference, Version, LastKnownIpAddress, LastSeenAtUtc, IsActive, CreatedAtUtc, UpdatedAtUtc)
  VALUES ('10000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000011', 'Berlin Monitoring Agent', @agentClientIdentifier, '1.0.0-demo', '10.10.20.15', DATEADD(minute, -2, @now), 1, @now, @now);

IF NOT EXISTS (SELECT 1 FROM ShellyDevices WHERE Id = '10000000-0000-0000-0000-000000000031')
  INSERT ShellyDevices (Id, AgentId, Name, DeviceType, BaseUrl, MacAddress, FirmwareVersion, IsVirtual, IsActive, CreatedAtUtc, UpdatedAtUtc)
  VALUES ('10000000-0000-0000-0000-000000000031', '10000000-0000-0000-0000-000000000021', 'Rack Row A Door Sensor', 'Shelly Door/Window 2', @shellyBaseUrl, '02:00:00:00:10:31', '2026.8-demo', 1, 1, @now, @now);

UPDATE ShellyDevices
SET BaseUrl = @shellyBaseUrl, AgentId = '10000000-0000-0000-0000-000000000021', IsActive = 1, UpdatedAtUtc = @now
WHERE Id = '10000000-0000-0000-0000-000000000031';

IF NOT EXISTS (SELECT 1 FROM MonitoredDevices WHERE Id = '10000000-0000-0000-0000-000000000041')
  INSERT MonitoredDevices (Id, AgentId, DisplayName, IpAddress, IntervalSeconds, TimeoutMilliseconds, FailureThreshold, IsActive, CreatedAtUtc, UpdatedAtUtc)
  VALUES ('10000000-0000-0000-0000-000000000041', '10000000-0000-0000-0000-000000000021', 'Core Gateway BER-01', '1.1.1.1', 30, 2000, 3, 1, @now, @now);

IF NOT EXISTS (SELECT 1 FROM MonitoredDevices WHERE Id = '10000000-0000-0000-0000-000000000042')
  INSERT MonitoredDevices (Id, AgentId, DisplayName, IpAddress, IntervalSeconds, TimeoutMilliseconds, FailureThreshold, IsActive, CreatedAtUtc, UpdatedAtUtc)
  VALUES ('10000000-0000-0000-0000-000000000042', '10000000-0000-0000-0000-000000000021', 'Agent Loopback Check', '127.0.0.1', 30, 1000, 3, 1, @now, @now);

UPDATE MonitoredDevices
SET DisplayName = 'Agent Loopback Check', IpAddress = '127.0.0.1', IsActive = 1, UpdatedAtUtc = @now
WHERE Id = '10000000-0000-0000-0000-000000000042';

IF NOT EXISTS (SELECT 1 FROM MaintenanceWindows WHERE Id = '10000000-0000-0000-0000-000000000051')
  INSERT MaintenanceWindows (Id, ServerRoomId, Title, StartUtc, EndUtc, Description, CreatedAtUtc, UpdatedAtUtc)
  VALUES ('10000000-0000-0000-0000-000000000051', '10000000-0000-0000-0000-000000000011', 'Quarterly UPS inspection', DATEADD(day, 2, @now), DATEADD(hour, 3, DATEADD(day, 2, @now)), 'Planned UPS inspection and battery load test by the facility team.', @now, @now);
""";

static string GetAuthSeedSql() => """
SET NOCOUNT ON;
IF EXISTS (SELECT 1 FROM AgentCredentials WHERE ClientIdentifier = @agentClientIdentifier)
  UPDATE AgentCredentials
  SET AgentId = '10000000-0000-0000-0000-000000000021', SecretHash = @secretHash, IsActive = 1, UpdatedAtUtc = @now
  WHERE ClientIdentifier = @agentClientIdentifier;
ELSE
  INSERT AgentCredentials (Id, AgentId, ClientIdentifier, SecretHash, IsActive, LastAuthenticatedAtUtc, CreatedAtUtc, UpdatedAtUtc)
  VALUES ('10000000-0000-0000-0000-0000000000B1','10000000-0000-0000-0000-000000000021',@agentClientIdentifier,@secretHash,1,NULL,@now,@now);
""";
