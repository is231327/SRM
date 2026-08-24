using SRMAgent.Configuration;
using SRMAgent.Services;
using SRMAgent.Services.Interfaces;
using SRMShared.Configuration;
using Scalar.AspNetCore;

namespace SRMAgent;

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

        builder.Services.Configure<AgentRuntimeOptions>(builder.Configuration.GetSection(AgentRuntimeOptions.SectionName));
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddHttpClient<IAgentAuthApiClient, AgentAuthApiClient>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(configuration["AgentApi:AuthBaseUrl"] ?? throw new InvalidOperationException("Missing configuration value 'AgentApi:AuthBaseUrl'."));
        });
        builder.Services.AddHttpClient<IAgentCoreApiClient, AgentCoreApiClient>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(configuration["AgentApi:CoreBaseUrl"] ?? throw new InvalidOperationException("Missing configuration value 'AgentApi:CoreBaseUrl'."));
        });
        builder.Services.AddHttpClient<IAgentRuntimeApiClient, AgentRuntimeApiClient>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(configuration["AgentApi:CoreBaseUrl"] ?? throw new InvalidOperationException("Missing configuration value 'AgentApi:CoreBaseUrl'."));
        });
        builder.Services.AddHttpClient<IVirtualShellyClient, VirtualShellyClient>();
        builder.Services.AddSingleton<AgentRuntimeCache>();
        builder.Services.AddSingleton<IRetryExecutor, RetryExecutor>();
        builder.Services.AddScoped<IMonitoredDevicePingService, MonitoredDevicePingService>();
        builder.Services.AddScoped<AgentMonitoringOrchestrator>();
        builder.Services.AddHostedService<AgentMonitoringWorker>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference(); // Add Scalar (like swagger ;-) )
            app.MapOpenApi();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
