namespace Integration.Tests;

/// <summary>
/// Tests the complete user journey through real HTTP endpoints:
///   Register → Dashboard (get referral code) → Click → Conversion → Dashboard (verify stats)
/// All steps run sequentially inside a single test so state flows naturally between them.
/// </summary>
public class FullJourneyTests : IClassFixture<IntegrationWebFactory>
{
    private readonly HttpClient _client;

    public FullJourneyTests(IntegrationWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullUserJourney_RegisterToConversion_DashboardReflectsStats()
    {
        // ── Step 1: Register ──────────────────────────────────────────────────
        var registerResp = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name     = "Journey User",
            email    = "journey@test.com",
            password = "JourneyPass1!"
        });

        Assert.Equal(HttpStatusCode.Created, registerResp.StatusCode);

        var authResult = await registerResp.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(authResult);
        Assert.NotEmpty(authResult!.Token);
        Assert.True(authResult.AffiliateId > 0);

        // Authenticate subsequent requests
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResult.Token);

        // ── Step 2: View dashboard — expect empty stats, capture referral code ─
        var dashboardResp = await _client.GetAsync("/api/affiliate/dashboard");

        Assert.Equal(HttpStatusCode.OK, dashboardResp.StatusCode);

        var dashboard = await dashboardResp.Content.ReadFromJsonAsync<DashboardResult>();
        Assert.NotNull(dashboard);
        Assert.Equal("Journey User", dashboard!.AffiliateName);
        Assert.Equal(8, dashboard.UniqueCode.Length);
        Assert.Equal(0, dashboard.TotalClicks);

        var affiliateCode = dashboard.UniqueCode;

        // ── Step 3: Record a new click (no existing session cookie) ───────────
        var clickResp = await _client.GetAsync($"/api/tracking/click?affiliateCode={affiliateCode}");

        Assert.Equal(HttpStatusCode.OK, clickResp.StatusCode);

        var clickResult = await clickResp.Content.ReadFromJsonAsync<ClickResult>();
        Assert.NotNull(clickResult);
        Assert.True(clickResult!.IsUnique);
        Assert.Equal(affiliateCode, clickResult.AffiliateCode);

        // Capture the session cookie returned by the server
        var setCookie = clickResp.Headers.Contains("Set-Cookie")
            ? clickResp.Headers.GetValues("Set-Cookie").FirstOrDefault()
            : null;
        var sessionId = ExtractSessionIdFromCookie(setCookie);

        // ── Step 4: Record a conversion attributed to this session ────────────
        var conversionResp = await _client.PostAsJsonAsync("/api/tracking/convert", new
        {
            sessionId      = sessionId ?? "fallback-session",
            conversionType = "Registration",
            userId         = (int?)null
        });

        Assert.Equal(HttpStatusCode.Created, conversionResp.StatusCode);

        var conversion = await conversionResp.Content.ReadFromJsonAsync<ConversionResult>();
        Assert.NotNull(conversion);
        Assert.True(conversion!.IsAttributed);
        Assert.Equal(affiliateCode, conversion.AffiliateCode);
        Assert.Equal("Registration", conversion.ConversionType);

        // ── Step 5: Dashboard now shows 1 click ───────────────────────────────
        var updatedDashResp = await _client.GetAsync("/api/affiliate/dashboard");
        var updatedDash = await updatedDashResp.Content.ReadFromJsonAsync<DashboardResult>();

        Assert.NotNull(updatedDash);
        Assert.Equal(1, updatedDash!.TotalClicks);
        Assert.Equal(1, updatedDash.UniqueClicks);
        Assert.Equal(1, updatedDash.Last7DayClicks);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var payload = new { name = "Alice", email = "alice-dup@test.com", password = "Pass123!" };
        await _client.PostAsJsonAsync("/api/auth/register", payload);

        var resp = await _client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "nobody@test.com",
            password = "anything"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Dashboard_WithoutToken_Returns401()
    {
        // Create a fresh client with no Authorization header
        var resp = await _client.GetAsync("/api/affiliate/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task RecordClick_UnknownCode_Returns404()
    {
        var resp = await _client.GetAsync("/api/tracking/click?affiliateCode=BADCODE1");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // Helper: extract the session ID value from Set-Cookie header
    private static string? ExtractSessionIdFromCookie(string? setCookieHeader)
    {
        if (setCookieHeader is null) return null;
        // Format: "aff_sid=<value>; path=/; ..."
        var parts = setCookieHeader.Split(';');
        var kvp = parts[0].Split('=', 2);
        return kvp.Length == 2 ? kvp[1] : null;
    }
}
