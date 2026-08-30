using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SRMCore.Configuration;
using SRMCore.Mappings;
using SRMCore.Mappings.Interfaces;
using SRMCore.Middleware;
using SRMCore.Security;
using SRMCore.Services;
using SRMCore.Services.Interfaces;
using SRMShared.Auth;
using SRMShared.Configuration;
using SRMShared.DTOs.Agent;
using SRMShared.DTOs.Customer;
using SRMShared.DTOs.Incident;
using SRMShared.DTOs.MaintenanceWindow;
using SRMShared.DTOs.MonitoredDevice;
using SRMShared.DTOs.MonitoredDevicePingResult;
using SRMShared.DTOs.SensorReading;
using SRMShared.DTOs.ServerRoom;
using SRMShared.DTOs.ShellyDevice;
using SRMShared.Entities;
using StackExchange.Redis;
using SRMCore.Data;

namespace SRMCore;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddInMemoryCollection(DevelopmentEnvironment.Load());
            builder.Configuration.AddEnvironmentVariables();
        }

        builder.Services.Configure<RedmineOptions>(builder.Configuration.GetSection(RedmineOptions.SectionName));
        builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDbContext<SrmCoreDbContext>(options =>
            options.UseSqlServer(
                ResolveCoreSqlConnectionString(builder.Configuration),
                sqlOptions => sqlOptions.EnableRetryOnFailure()));
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(ResolveRedisConnectionString(builder.Configuration)));
        builder.Services.AddSingleton<ITokenStateStore, RedisTokenStateStore>();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"] ?? string.Empty)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var tokenJti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        if (string.IsNullOrWhiteSpace(tokenJti))
                        {
                            context.Fail("Missing token jti claim.");
                            return;
                        }

                        var tokenStateStore = context.HttpContext.RequestServices.GetRequiredService<ITokenStateStore>();
                        var revoked = await tokenStateStore.IsAccessTokenRevokedAsync(tokenJti, context.HttpContext.RequestAborted);
                        if (revoked)
                        {
                            context.Fail("The access token has been revoked.");
                        }
                    }
                };
            });
        builder.Services.AddAuthorization();
        builder.Services.AddHealthChecks();
        builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        builder.Services.AddScoped<ICustomerService, CustomerService>();
        builder.Services.AddScoped<IServerRoomService, ServerRoomService>();
        builder.Services.AddScoped<IAgentService, AgentService>();
        builder.Services.AddScoped<IAgentReportingService, AgentReportingService>();
        builder.Services.AddScoped<IAgentRuntimeService, AgentRuntimeService>();
        builder.Services.AddScoped<IIncidentService, IncidentService>();
        builder.Services.AddScoped<IIncidentQueryService, IncidentQueryService>();
        builder.Services.AddScoped<ITicketDispatchService, TicketDispatchService>();
        builder.Services.AddHttpClient<IRedmineTicketingClient, RedmineTicketingClient>();
        builder.Services.AddHostedService<RedmineTicketWorker>();
        builder.Services.AddScoped<IShellyDeviceService, ShellyDeviceService>();
        builder.Services.AddScoped<IMonitoredDeviceService, MonitoredDeviceService>();
        builder.Services.AddScoped<IMonitoredDevicePingResultService, MonitoredDevicePingResultService>();
        builder.Services.AddScoped<IMaintenanceWindowService, MaintenanceWindowService>();
        builder.Services.AddScoped<ISensorReadingService, SensorReadingService>();
        builder.Services.AddScoped<ICrudDtoMapper<Customer, CustomerCreateDto, CustomerUpdateDto, CustomerReadDto>, CustomerDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<ServerRoom, ServerRoomCreateDto, ServerRoomUpdateDto, ServerRoomReadDto>, ServerRoomDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<Agent, AgentCreateDto, AgentUpdateDto, AgentReadDto>, AgentDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<ShellyDevice, ShellyDeviceCreateDto, ShellyDeviceUpdateDto, ShellyDeviceReadDto>, ShellyDeviceDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<MonitoredDevice, MonitoredDeviceCreateDto, MonitoredDeviceUpdateDto, MonitoredDeviceReadDto>, MonitoredDeviceDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<MonitoredDevicePingResult, MonitoredDevicePingResultCreateDto, MonitoredDevicePingResultUpdateDto, MonitoredDevicePingResultReadDto>, MonitoredDevicePingResultDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<MaintenanceWindow, MaintenanceWindowCreateDto, MaintenanceWindowUpdateDto, MaintenanceWindowReadDto>, MaintenanceWindowDtoMapper>();
        builder.Services.AddScoped<ICrudDtoMapper<SensorReading, SensorReadingCreateDto, SensorReadingUpdateDto, SensorReadingReadDto>, SensorReadingDtoMapper>();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        InitializeDatabaseWithRetry(app.Services);

        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference();
            app.MapOpenApi();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }
        app.UseMiddleware<AuthorizationExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");

        app.Run();
    }

    private static void InitializeDatabaseWithRetry(IServiceProvider services)
    {
        const int maxAttempts = 12;
        var delay = TimeSpan.FromSeconds(5);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SrmCoreDbContext>();
                dbContext.Database.EnsureCreated();
                logger.LogInformation("SRMCore database initialization completed on attempt {Attempt}.", attempt);
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    exception,
                    "SRMCore database initialization attempt {Attempt} of {MaxAttempts} failed. Retrying in {DelaySeconds} seconds.",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);
                Thread.Sleep(delay);
            }
        }

        using var finalScope = services.CreateScope();
        var finalLogger = finalScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            var dbContext = finalScope.ServiceProvider.GetRequiredService<SrmCoreDbContext>();
            dbContext.Database.EnsureCreated();
        }
        catch (Exception exception)
        {
            finalLogger.LogError(exception, "SRMCore database initialization failed after all retry attempts.");
            throw;
        }
    }
    private static string ResolveCoreSqlConnectionString(IConfiguration configuration)
    {
        var connectionString = SqlServerConnectionStringFactory.Resolve(
            configuration,
            connectionStringName: "SrmCoreDatabase",
            connectionStringEnvironmentKey: null,
            databaseEnvironmentKey: "SRM_SQL_CORE_DATABASE");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException(
            "Missing core SQL connection configuration. Provide either 'ConnectionStrings:SrmCoreDatabase' or the split SQL settings 'SRM_SQL_HOST', 'SRM_SQL_PORT', 'SRM_SQL_USERNAME', 'MSSQL_SA_PASSWORD', and 'SRM_SQL_CORE_DATABASE'.");
    }
    private static string ResolveRedisConnectionString(IConfiguration configuration)
    {
        var redisConnectionString = configuration["Redis:ConnectionString"]
            ?? configuration["SRM_REDIS_CONNECTION"]
            ?? Environment.GetEnvironmentVariable("SRM_REDIS_CONNECTION");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            return redisConnectionString;
        }

        throw new InvalidOperationException(
            "Missing Redis connection configuration. Provide either 'Redis:ConnectionString' or 'SRM_REDIS_CONNECTION'. " +
            "For local development, start the Redis infrastructure container and configure ContainerServices/.env.development.");
    }
}
