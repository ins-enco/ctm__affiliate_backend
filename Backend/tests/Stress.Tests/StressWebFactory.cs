extern alias Host;

using System.Text;
using Auth.Application.Settings;
using Affiliate.Infrastructure.Persistence;
using Auth.Infrastructure.Persistence;
using Tracking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Stress.Tests;

/// <summary>
/// Spins up the full ASP.NET Core application in-process against a dedicated
/// stress database — completely isolated from copytrade_db.
///
/// Connection string is read from appsettings.json → StressDb:ConnectionString.
/// Schema is auto-created via Migrate() on first run. Data accumulates across
/// runs (no flush). Drop the DB manually if you need a clean slate.
/// </summary>
public class StressWebFactory : WebApplicationFactory<Host::Program>
{
    internal const string StressSecretKey = "stress-secret-key-for-jwt-signing-copytrade-123456";

    // Reads from Stress.Tests/appsettings.json. Override with STRESS_CONNECTION_STRING env var.
    internal static readonly string StressConnectionString =
        Environment.GetEnvironmentVariable("STRESS_CONNECTION_STRING")
        ?? new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build()["StressDb:ConnectionString"]
        ?? throw new InvalidOperationException(
            "StressDb:ConnectionString missing from appsettings.json.");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Suppress all logging — at 1M req/s Serilog floods the console with
        // EF Core warnings (duplicate key) and request logs, blocking the
        // reporter thread on the console lock and freezing the live display.
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureTestServices(services =>
        {
            // ── Replace all three DbContexts to point at the stress DB ────────
            // Same pattern as IntegrationWebFactory but with MySQL instead of SQLite.
            ReplaceWithMySql<AuthDbContext>     (services, StressConnectionString);
            ReplaceWithMySql<AffiliateDbContext>(services, StressConnectionString);
            ReplaceWithMySql<TrackingDbContext> (services, StressConnectionString);

            // ── Override JWT signing key ──────────────────────────────────────
            var existing = services.SingleOrDefault(d => d.ServiceType == typeof(JwtSettings));
            if (existing != null) services.Remove(existing);

            services.AddSingleton(new JwtSettings
            {
                Issuer        = "CopyTradeMarketApi",
                Audience      = "CopyTradeMarketApiClients",
                ExpiryMinutes = 60,
                SecretKey     = StressSecretKey,
            });

            // ── Override JWT Bearer validation key ────────────────────────────
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey         = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(StressSecretKey)),
                        ValidateIssuer   = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                    };
                });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Explicitly run migrations so the stress DB + schema are created
        // before any scenario hits the API. MigrateAsync() creates the
        // database if it does not exist yet (Pomelo handles CREATE DATABASE).
        Console.WriteLine($"[StressWebFactory] Running migrations on: {StressConnectionString}");

        using var scope = host.Services.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetRequiredService<AuthDbContext>()     .Database.Migrate();
        sp.GetRequiredService<AffiliateDbContext>().Database.Migrate();
        sp.GetRequiredService<TrackingDbContext>() .Database.Migrate();

        Console.WriteLine("[StressWebFactory] Migrations complete — copytrade_stress_db ready.");
        return host;
    }

    private static void ReplaceWithMySql<TContext>(IServiceCollection services, string connectionString)
        where TContext : DbContext
    {
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<TContext>));
        if (descriptor != null) services.Remove(descriptor);

        services.AddDbContext<TContext>(opt =>
            opt.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));
    }
}
