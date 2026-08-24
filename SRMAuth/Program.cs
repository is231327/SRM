using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SRMAuth.Configuration;
using SRMAuth.Data;
using SRMAuth.Middleware;
using SRMAuth.Security;
using SRMAuth.Services;
using SRMAuth.Services.Interfaces;
using SRMShared.Auth;
using SRMShared.Configuration;
using SRMShared.Entities;
using StackExchange.Redis;

namespace SRMAuth;

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

        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDbContext<SrmAuthDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("SrmAuthDatabase"),
                sqlOptions => sqlOptions.EnableRetryOnFailure()));
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]
                ?? throw new InvalidOperationException("Missing configuration value 'Redis:ConnectionString'.")));
        builder.Services.AddSingleton<ITokenStateStore, RedisTokenStateStore>();
        builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        builder.Services.AddScoped<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
        builder.Services.AddScoped<IAuthService, AuthService>();

        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
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
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SrmAuthDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AuthUser>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            dbContext.Database.EnsureCreated();
            AuthDbSeeder.SeedAsync(dbContext, passwordHasher, configuration).GetAwaiter().GetResult();
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
