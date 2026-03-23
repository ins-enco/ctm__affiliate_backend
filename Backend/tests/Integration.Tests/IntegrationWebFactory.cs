using System.Text;
using Auth.Application.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.Tokens;
using Affiliate.Infrastructure.Persistence;
using Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tracking.Infrastructure.Persistence;

namespace Integration.Tests;

/// <summary>
/// Spins up the full ASP.NET Core application, replacing all three MySQL
/// DbContexts with isolated EF Core InMemory databases.
/// Each factory instance gets its own unique database names so test classes
/// never share state.
/// </summary>
public class IntegrationWebFactory : WebApplicationFactory<Program>
{
    // The same key is used by AuthService (via JwtSettings singleton override) AND
    // by the JWT Bearer middleware (via PostConfigure below), so tokens validate correctly.
    internal const string TestSecretKey = "super-secret-key-for-integration-tests-1234567890";

    private readonly string _dbSuffix = Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override config so modules don't throw on missing connection string / JWT settings
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=test_integration;",
                ["JwtSettings:Issuer"]                  = "CopyTradeMarketApi",
                ["JwtSettings:Audience"]                = "CopyTradeMarketApiClients",
                ["JwtSettings:SecretKey"]               = TestSecretKey,
                ["JwtSettings:ExpiryMinutes"]           = "60",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace all three MySQL DbContexts with InMemory
            ReplaceWithInMemory<AffiliateDbContext>(services, $"integration_affiliate_{_dbSuffix}");
            ReplaceWithInMemory<AuthDbContext>     (services, $"integration_auth_{_dbSuffix}");
            ReplaceWithInMemory<TrackingDbContext> (services, $"integration_tracking_{_dbSuffix}");

            // Replace JwtSettings singleton so AuthService signs tokens with TestSecretKey.
            // (AuthModule.RegisterServices already registered one from appsettings.json)
            var jwtDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(JwtSettings));
            if (jwtDescriptor != null) services.Remove(jwtDescriptor);

            services.AddSingleton(new JwtSettings
            {
                Issuer        = "CopyTradeMarketApi",
                Audience      = "CopyTradeMarketApiClients",
                ExpiryMinutes = 60,
                SecretKey     = TestSecretKey,
            });

            // Override the JWT Bearer validation key so the middleware accepts tokens
            // signed with TestSecretKey (Program.cs may have captured the appsettings value).
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey         = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(TestSecretKey)),
                        ValidateIssuer           = false,
                        ValidateAudience         = false,
                        ValidateLifetime         = true,
                    };
                });
        });
    }

    private static void ReplaceWithInMemory<TContext>(IServiceCollection services, string dbName)
        where TContext : DbContext
    {
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<TContext>));
        if (descriptor != null)
            services.Remove(descriptor);

        services.AddDbContext<TContext>(opt => opt.UseInMemoryDatabase(dbName));
    }
}
