namespace Integration.Tests;

/// <summary>
/// Spins up the full ASP.NET Core application, replacing all three MySQL
/// DbContexts with isolated SQLite in-memory databases.
/// SQLite enforces unique indexes — required for DbUpdateException duplicate detection.
/// Each factory instance keeps its own open connections so in-memory databases persist
/// for the lifetime of the factory.
/// </summary>
public class IntegrationWebFactory : WebApplicationFactory<Program>
{
    // The same key is used by AuthService (via JwtSettings singleton override) AND
    // by the JWT Bearer middleware (via PostConfigure below), so tokens validate correctly.
    internal const string TestSecretKey = "super-secret-key-for-integration-tests-1234567890";

    private readonly SqliteConnection _affiliateConn = new("Data Source=:memory:");
    private readonly SqliteConnection _authConn      = new("Data Source=:memory:");
    private readonly SqliteConnection _trackingConn  = new("Data Source=:memory:");

    public IntegrationWebFactory()
    {
        // Keep connections open so the in-memory databases persist for the factory lifetime
        _affiliateConn.Open();
        _authConn.Open();
        _trackingConn.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use "Testing" environment so Program.cs skips DevDataSeeder (which requires existing tables)
        builder.UseEnvironment("Testing");

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
            // Replace all three MySQL DbContexts with SQLite in-memory
            ReplaceWithSqlite<AffiliateDbContext>(services, _affiliateConn);
            ReplaceWithSqlite<AuthDbContext>     (services, _authConn);
            ReplaceWithSqlite<TrackingDbContext> (services, _trackingConn);

            // Replace JwtSettings singleton so AuthService signs tokens with TestSecretKey.
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

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Create the schema (tables + indexes) for all three DbContexts.
        // EnsureCreated() applies EF model including unique indexes — required for
        // DbUpdateException duplicate detection in TrackingService.
        using var scope = host.Services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<AuthDbContext>().Database.EnsureCreated();
        sp.GetRequiredService<AffiliateDbContext>().Database.EnsureCreated();
        sp.GetRequiredService<TrackingDbContext>().Database.EnsureCreated();

        return host;
    }

    private static void ReplaceWithSqlite<TContext>(IServiceCollection services, SqliteConnection connection)
        where TContext : DbContext
    {
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<TContext>));
        if (descriptor != null)
            services.Remove(descriptor);

        services.AddDbContext<TContext>(opt => opt.UseSqlite(connection));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _affiliateConn.Dispose();
            _authConn.Dispose();
            _trackingConn.Dispose();
        }
        base.Dispose(disposing);
    }
}
