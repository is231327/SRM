using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using SRMApp.Components;
using SRMApp.Localization;
using SRMApp.Services;
using SRMShared.Configuration;

namespace SRMApp;

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

        builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));

        var redisConnectionString = ResolveRedisConnectionString(builder.Configuration);
        var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

        builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
        builder.Services.AddDataProtection()
            .SetApplicationName("SRMApp")
            .PersistKeysToStackExchangeRedis(redisConnection, "SRMApp-DataProtection-Keys");

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddHttpClient<ICoreApiClient, CoreApiClient>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(configuration["CoreApi:BaseUrl"] ?? throw new InvalidOperationException("Missing configuration value 'CoreApi:BaseUrl'."));
        });
        builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(configuration["AuthApi:BaseUrl"] ?? throw new InvalidOperationException("Missing configuration value 'AuthApi:BaseUrl'."));
        });
        builder.Services.AddScoped<ProtectedLocalStorage>();
        builder.Services.AddScoped<ProtectedSessionStorage>();
        builder.Services.AddScoped<IAuthSessionStore, ProtectedBrowserAuthSessionStore>();
        builder.Services.AddScoped<AuthSessionService>();
        builder.Services.AddScoped<LanguageService>();
        builder.Services.AddScoped<IOverviewDataService, OverviewDataService>();
        builder.Services.AddScoped<ICrudPageDataService, CrudPageDataService>();
        builder.Services.AddHealthChecks();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.MapHealthChecks("/health");

        app.Run();
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
