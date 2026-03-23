namespace Integration.Tests;

/// <summary>
/// Factory that seeds the database once, before the first test in the class runs.
/// Implements IAsyncLifetime so xUnit calls InitializeAsync before any test method.
/// </summary>
public class SeededIntegrationFactory : IntegrationWebFactory, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
    }

    public async Task InitializeAsync()
    {
        // CreateClient() forces the factory to build the host and DI container.
        // After this call, Services is available for seeding.
        CreateClient();
        await TestDataSeeder.SeedAsync(Services);
    }

    public new Task DisposeAsync() => Task.CompletedTask;
}

/// <summary>
/// Scenario tests that depend on the pre-seeded database.
/// The SeededIntegrationFactory is created once per test class.
/// </summary>
public class SeededScenarioTests : IClassFixture<SeededIntegrationFactory>
{
    private readonly HttpClient _client;

    public SeededScenarioTests(SeededIntegrationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Auth ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_SeededCredentials_Returns200WithToken()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = TestDataSeeder.SeededEmail,
            password = TestDataSeeder.SeededPassword
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var result = await resp.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(result);
        Assert.NotEmpty(result!.Token);
        Assert.Equal(TestDataSeeder.SeededAffiliateId, result.AffiliateId);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = TestDataSeeder.SeededEmail,
            password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_SeededAffiliate_ReturnsCorrectClickStats()
    {
        var token = await LoginSeededUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.GetAsync("/api/affiliate/dashboard");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dashboard = await resp.Content.ReadFromJsonAsync<DashboardResult>();
        Assert.NotNull(dashboard);

        // Based on TestDataSeeder: 5 total rows, 4 unique, 3 unique within last 7 days
        Assert.Equal(TestDataSeeder.SeededCode, dashboard!.UniqueCode);
        Assert.Equal(5, dashboard.TotalClicks);
        Assert.Equal(4, dashboard.UniqueClicks);
        Assert.Equal(3, dashboard.Last7DayClicks);
    }

    // ── Tracking ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordClick_SeededCode_NewSession_IsUnique()
    {
        var resp = await _client.GetAsync(
            $"/api/tracking/click?affiliateCode={TestDataSeeder.SeededCode}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var result = await resp.Content.ReadFromJsonAsync<Tracking.Application.DTOs.ClickResult>();
        Assert.NotNull(result);
        Assert.True(result!.IsUnique);
    }

    [Fact]
    public async Task RecordClick_SeededCode_SameSessionAsSeedData_IsNotUnique()
    {
        // "SES-A" was already seeded for this affiliate → repeat visit
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/tracking/click?affiliateCode={TestDataSeeder.SeededCode}");
        request.Headers.Add("Cookie", "aff_sid=SES-A");

        var resp = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var result = await resp.Content.ReadFromJsonAsync<Tracking.Application.DTOs.ClickResult>();
        Assert.NotNull(result);
        Assert.False(result!.IsUnique);
    }

    [Fact]
    public async Task RecordConversion_NoMatchingClick_IsUnattributed()
    {
        var resp = await _client.PostAsJsonAsync("/api/tracking/convert", new
        {
            sessionId      = "SESSION-ORPHAN",
            conversionType = "Deposit",
            userId         = (int?)null
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var result = await resp.Content.ReadFromJsonAsync<Tracking.Application.DTOs.ConversionResult>();
        Assert.NotNull(result);
        Assert.False(result!.IsAttributed);
        Assert.Null(result.AffiliateCode);
    }

    [Fact]
    public async Task RecordConversion_DuplicateForSameSession_Returns409()
    {
        const string session = "SESSION-DUP-SEEDED";

        await _client.PostAsJsonAsync("/api/tracking/convert", new
        {
            sessionId      = session,
            conversionType = "Registration",
            userId         = (int?)null
        });

        var resp = await _client.PostAsJsonAsync("/api/tracking/convert", new
        {
            sessionId      = session,
            conversionType = "Registration",
            userId         = (int?)null
        });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> LoginSeededUserAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = TestDataSeeder.SeededEmail,
            password = TestDataSeeder.SeededPassword
        });
        var result = await resp.Content.ReadFromJsonAsync<AuthResult>();
        return result!.Token;
    }
}
