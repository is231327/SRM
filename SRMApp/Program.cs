using SRMApp.Components;
using SRMApp.Localization;
using SRMApp.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace SRMApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
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
        builder.Services.AddScoped<ProtectedSessionStorage>();
        builder.Services.AddScoped<AuthSessionService>();
        builder.Services.AddScoped<LanguageService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
