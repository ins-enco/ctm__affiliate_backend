using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests;

/// <summary>
/// Verifies the Observer Pattern (domain event) integration:
///   Affiliate click → visitor registers with aff_sid cookie →
///   UserRegisteredEvent fires → UserRegisteredEventHandler records
///   a "Registration" conversion automatically — no explicit call to /convert.
/// </summary>
public class ObserverPatternTests : IClassFixture<IntegrationWebFactory>
{
    private readonly IntegrationWebFactory _factory;

    public ObserverPatternTests(IntegrationWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithAffSidCookie_AutomaticallyRecordsRegistrationConversion()
    {
        var client = _factory.CreateClient();

        // ── Step 1: Affiliate registers and gets their referral code ──────────
        var affiliateResp = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name     = "Observer Affiliate",
            email    = "obs-affiliate@test.com",
            password = "AffPass123!"
        });
        Assert.Equal(HttpStatusCode.Created, affiliateResp.StatusCode);

        var affiliateAuth = await affiliateResp.Content.ReadFromJsonAsync<AuthResult>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", affiliateAuth!.Token);

        var dashboardResp = await client.GetAsync("/api/affiliate/dashboard");
        var dashboard = await dashboardResp.Content.ReadFromJsonAsync<DashboardResult>();
        var affiliateCode = dashboard!.UniqueCode;

        // ── Step 2: Visitor clicks the affiliate link — captures session ID ───
        var clickResp = await client.GetAsync($"/api/tracking/click?affiliateCode={affiliateCode}");
        Assert.Equal(HttpStatusCode.OK, clickResp.StatusCode);
        Assert.True(clickResp.Headers.Contains("Set-Cookie"));

        var setCookie = clickResp.Headers.GetValues("Set-Cookie").First();
        var sessionId = ExtractSessionIdFromCookie(setCookie);
        Assert.NotNull(sessionId);

        // ── Step 3: Visitor registers WITH the aff_sid cookie ─────────────────
        var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
        registerRequest.Headers.Add("Cookie", $"aff_sid={sessionId}");
        registerRequest.Content = JsonContent.Create(new
        {
            name     = "Observer Visitor",
            email    = "obs-visitor@test.com",
            password = "VisPass123!"
        });

        var registerResp = await client.SendAsync(registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResp.StatusCode);

        // ── Step 4: Verify conversion was auto-recorded in DB ─────────────────
        using var scope = _factory.Services.CreateScope();
        var trackingDb = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();

        var conversion = await trackingDb.ConversionEvents
            .FirstOrDefaultAsync(e => e.SessionId == sessionId && e.ConversionType == "Registration");

        Assert.NotNull(conversion);
        Assert.Equal("Registration", conversion!.ConversionType);
    }

    [Fact]
    public async Task Register_WithoutAffSidCookie_NoConversionRecorded()
    {
        var client = _factory.CreateClient();

        // ── Snapshot conversion count before registering ─────────────────────
        using var scopeBefore = _factory.Services.CreateScope();
        var countBefore = await scopeBefore.ServiceProvider
            .GetRequiredService<TrackingDbContext>()
            .ConversionEvents.CountAsync();

        // ── Register (no cookie) ──────────────────────────────────────────────
        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name     = "No-Cookie Visitor",
            email    = "nocookie-visitor@test.com",
            password = "VisPass123!"
        });
        Assert.Equal(HttpStatusCode.Created, registerResp.StatusCode);

        // ── Verify count did not increase ─────────────────────────────────────
        using var scopeAfter = _factory.Services.CreateScope();
        var countAfter = await scopeAfter.ServiceProvider
            .GetRequiredService<TrackingDbContext>()
            .ConversionEvents.CountAsync();

        Assert.Equal(countBefore, countAfter);
    }

    private static string? ExtractSessionIdFromCookie(string setCookieHeader)
    {
        var parts = setCookieHeader.Split(';');
        var kvp = parts[0].Split('=', 2);
        return kvp.Length == 2 ? kvp[1] : null;
    }
}
