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
                builder.Configuration.GetConnectionString("SrmCoreDatabase"),
                sqlOptions => sqlOptions.EnableRetryOnFailure()));
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]
                ?? throw new InvalidOperationException("Missing configuration value 'Redis:ConnectionString'.")));
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

        using (IServiceScope scope = app.Services.CreateScope())
        {
            SrmCoreDbContext dbContext = scope.ServiceProvider.GetRequiredService<SrmCoreDbContext>();
            dbContext.Database.EnsureCreated();
        }

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

        app.Run();
    }
}
