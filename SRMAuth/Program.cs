using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SRMAuth.Configuration;
using SRMAuth.Data;
using SRMAuth.Middleware;
using SRMAuth.Security;
using SRMAuth.Services;
using SRMAuth.Services.Interfaces;
using Scalar.AspNetCore;
using SRMShared.Entities;

namespace SRMAuth;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDbContext<SrmAuthDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("SrmAuthDatabase")));
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
            app.MapScalarApiReference(); // Add Scalar (like swagger ;-) )
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseMiddleware<AuthorizationExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
