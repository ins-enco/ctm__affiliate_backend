using Tracking.Application.Services;
using CopyTradeMarketApi.Shared.Abstractions;

namespace Integration.Tests;

// ── Controllable TrackingService ─────────────────────────────────────────────
// Overrides GetAttributionBucket() so tests can pin the month without advancing
// the real clock. The factory reads CurrentBucket at scope-creation time (per
// request), so changing it between requests works correctly.

internal sealed class BucketOverrideTrackingService(
    TrackingDbContext db,
    IAffiliateLookupService lookup,
    ICacheService cache,
    ILogger<TrackingService> logger,
    Func<string> getBucket)
    : TrackingService(db, lookup, cache, logger)
{
    protected override string GetAttributionBucket() => getBucket();
}

// ── Factory ───────────────────────────────────────────────────────────────────
public class AttributionWindowFactory : IntegrationWebFactory
{
    public string CurrentBucket { get; set; } = "2025-01";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            var desc = services.FirstOrDefault(d => d.ServiceType == typeof(ITrackingService));
            if (desc != null) services.Remove(desc);

            services.AddScoped<ITrackingService>(sp =>
                new BucketOverrideTrackingService(
                    sp.GetRequiredService<TrackingDbContext>(),
                    sp.GetRequiredService<IAffiliateLookupService>(),
                    sp.GetRequiredService<ICacheService>(),
                    sp.GetRequiredService<ILogger<TrackingService>>(),
                    () => CurrentBucket));
        });
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────
/// <summary>
/// Verifies that the monthly attribution bucket is included in the session hash.
/// Same IP + UA + code in the same bucket → same hash → DB unique index blocks the second click.
/// Same IP + UA + code in a new bucket  → new hash → counted as a fresh unique click.
/// </summary>
public class AttributionWindowTests : IClassFixture<AttributionWindowFactory>
{
    private readonly AttributionWindowFactory _factory;

    public AttributionWindowTests(AttributionWindowFactory factory)
    {
        _factory = factory;
    }

    // Registers a fresh affiliate and returns their unique referral code.
    private async Task<string> CreateAffiliateCodeAsync()
    {
        using var client = _factory.CreateClient();
        var email = $"attr_{Guid.NewGuid():N}@test.com";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        {
            userInformation = new
            {
                firstName   = "Attr",
                lastName    = "Test",
                email       = email,
                phoneCode   = "+84",
                phoneNumber = "901234567",
                language    = "vi"
            },
            password        = "AttrPass1!",
            confirmPassword = "AttrPass1!"
        });
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);

        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = email,
            password = "AttrPass1!"
        });
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResult>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);
        var dash = await client.GetFromJsonAsync<DashboardResult>("/api/affiliate/dashboard");
        return dash!.UniqueCode;
    }

    [Fact]
    public async Task SameIdentity_SameBucket_NoCookie_SecondClickIsNotUnique()
    {
        // Arrange — two clients so neither carries the aff_sid cookie from the other
        var code = await CreateAffiliateCodeAsync();
        _factory.CurrentBucket = "2025-01";

        using var client1 = _factory.CreateClient();
        using var client2 = _factory.CreateClient();

        var req1 = ClickRequest(code, "10.20.30.40", "TestBrowser/1.0");
        var r1 = await (await client1.SendAsync(req1))
            .Content.ReadFromJsonAsync<ClickResult>();

        // Act — fresh client (no cookie), same IP+UA+code+bucket → same hash
        var req2 = ClickRequest(code, "10.20.30.40", "TestBrowser/1.0");
        var r2 = await (await client2.SendAsync(req2))
            .Content.ReadFromJsonAsync<ClickResult>();

        Assert.True(r1!.IsUnique);
        Assert.False(r2!.IsUnique); // same hash → DB unique index blocks it
    }

    [Fact]
    public async Task SameIdentity_NewBucket_IsUnique()
    {
        // Arrange
        var code = await CreateAffiliateCodeAsync();

        // Click in month 1
        _factory.CurrentBucket = "2025-01";
        using var client1 = _factory.CreateClient();
        await client1.SendAsync(ClickRequest(code, "10.20.30.41", "TestBrowser/2.0"));

        // Act — same IP+UA but new month → different bucket → different hash
        _factory.CurrentBucket = "2025-02";
        using var client2 = _factory.CreateClient();
        var r2 = await (await client2.SendAsync(ClickRequest(code, "10.20.30.41", "TestBrowser/2.0")))
            .Content.ReadFromJsonAsync<ClickResult>();

        Assert.True(r2!.IsUnique); // new bucket → new hash → counted as fresh unique click
    }

    private static HttpRequestMessage ClickRequest(string code, string ip, string ua)
    {
        var req = new HttpRequestMessage(HttpMethod.Get,
            $"/api/tracking/click?affiliateCode={code}");
        req.Headers.Add("X-Forwarded-For", ip);
        req.Headers.TryAddWithoutValidation("User-Agent", ua);
        return req;
    }
}
