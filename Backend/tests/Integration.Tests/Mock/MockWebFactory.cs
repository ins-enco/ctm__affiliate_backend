namespace Integration.Tests.Mock;

/// <summary>
/// WebApplicationFactory that runs the host in the Development environment,
/// enabling the Mock module endpoints (FR-011).
/// Applies the same SQLite + JWT overrides as <see cref="IntegrationWebFactory"/>.
/// </summary>
public class MockWebFactory : WebApplicationFactory<Program>
{
    internal const string TestSecretKey = IntegrationWebFactory.TestSecretKey;

    private readonly SqliteConnection _affiliateConn = new("Data Source=:memory:");
    private readonly SqliteConnection _authConn      = new("Data Source=:memory:");
    private readonly SqliteConnection _trackingConn  = new("Data Source=:memory:");

    public MockWebFactory()
    {
        _affiliateConn.Open();
        _authConn.Open();
        _trackingConn.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development environment so Program.cs registers MockModule
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=test_mock;",
                ["JwtSettings:Issuer"]                  = "CopyTradeMarketApi",
                ["JwtSettings:Audience"]                = "CopyTradeMarketApiClients",
                ["JwtSettings:SecretKey"]               = TestSecretKey,
                ["JwtSettings:ExpiryMinutes"]           = "60",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            ReplaceWithSqlite<AffiliateDbContext>(services, _affiliateConn);
            ReplaceWithSqlite<AuthDbContext>     (services, _authConn);
            ReplaceWithSqlite<TrackingDbContext> (services, _trackingConn);

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
