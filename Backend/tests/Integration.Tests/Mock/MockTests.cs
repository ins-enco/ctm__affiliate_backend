using Mock.Application.DTOs;

namespace Integration.Tests.Mock;

/// <summary>
/// Integration tests for the Mock module endpoints (Development environment).
/// Uses MockWebFactory which runs the host with IsDevelopment() = true.
/// </summary>
public class MockTests : IClassFixture<MockWebFactory>
{
    private static readonly string[] AllowedRoles       = ["Client", "Signal Provider", "Affiliate"];
    private static readonly string[] AllowedKycStatuses = ["Pending", "Verified", "Rejected"];

    private readonly HttpClient _client;

    public MockTests(MockWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── User Story 1: GET /api/mock/users ─────────────────────────────────────

    [Fact]
    public async Task GetUsers_Returns200WithAtLeastFiveUsers()
    {
        var resp = await _client.GetAsync("/api/dashboard/listOfUsers");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PagedResponse<UserDto>>();
        Assert.NotNull(body);
        Assert.True(body.Items.Count >= 5, $"Expected ≥5 users, got {body.Items.Count}");
        Assert.True(body.TotalCount >= 5);
        Assert.Null(body.Page);
    }

    [Fact]
    public async Task GetUsers_CoversAllThreeRoles()
    {
        var body = await _client.GetFromJsonAsync<PagedResponse<UserDto>>("/api/dashboard/listOfUsers");

        Assert.NotNull(body);
        Assert.Contains(body.Items, u => u.Role == "Client");
        Assert.Contains(body.Items, u => u.Role == "Signal Provider");
        Assert.Contains(body.Items, u => u.Role == "Affiliate");
    }

    [Fact]
    public async Task GetUsers_AllRolesFromAllowedSet()
    {
        var body = await _client.GetFromJsonAsync<PagedResponse<UserDto>>("/api/dashboard/listOfUsers");

        Assert.NotNull(body);
        Assert.All(body.Items, u => Assert.Contains(u.Role, AllowedRoles));
    }

    // ── User Story 2: GET /api/currentActiveUser ─────────────────────────────

    [Fact]
    public async Task GetCurrentUser_Returns200WithSingleObject()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/currentActiveUser");
        req.Headers.Add("API-KEY", "SimulatedKeyForDev");
        var resp = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetCurrentUser_AbbreviationIsExactlyTwoChars()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/currentActiveUser");
        req.Headers.Add("API-KEY", "SimulatedKeyForDev");
        var resp = await _client.SendAsync(req);
        var body = await resp.Content.ReadFromJsonAsync<CurrentUserDto>();

        Assert.NotNull(body);
        Assert.Equal(2, body.Abbreviation.Length);
    }

    [Fact]
    public async Task GetCurrentUser_RoleIsFromAllowedSet()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/currentActiveUser");
        req.Headers.Add("API-KEY", "SimulatedKeyForDev");
        var resp = await _client.SendAsync(req);
        var body = await resp.Content.ReadFromJsonAsync<CurrentUserDto>();

        Assert.NotNull(body);
        Assert.Contains(body.Role, AllowedRoles);
    }

    [Fact]
    public async Task GetCurrentUser_MissingApiKey_Returns401()
    {
        var resp = await _client.GetAsync("/api/currentActiveUser");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WrongApiKey_Returns401()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/currentActiveUser");
        req.Headers.Add("API-KEY", "wrong-key");
        var resp = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── User Story 3: GET /api/dashboard/clientRequests ───────────────────────

    [Fact]
    public async Task GetClientRequests_Returns200WithExactlyTenRecords()
    {
        var resp = await _client.GetAsync("/api/dashboard/clientRequests");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PagedResponse<ClientRequestDto>>();
        Assert.NotNull(body);
        Assert.Equal(10, body.Items.Count);
        Assert.Equal(10, body.TotalCount);
    }

    [Fact]
    public async Task GetClientRequests_AllEquityValuesArePositive()
    {
        var body = await _client.GetFromJsonAsync<PagedResponse<ClientRequestDto>>("/api/dashboard/clientRequests");

        Assert.NotNull(body);
        Assert.All(body.Items, r => Assert.True(r.Equity > 0));
    }

    // ── User Story 4: GET /api/dashboard/signalProviderRequests ─────────────────

    [Fact]
    public async Task GetSignalProviderRequests_Returns200WithExactlyTenRecords()
    {
        var resp = await _client.GetAsync("/api/dashboard/signalProviderRequests");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PagedResponse<SignalProviderRequestDto>>();
        Assert.NotNull(body);
        Assert.Equal(10, body.Items.Count);
    }

    [Fact]
    public async Task GetSignalProviderRequests_AllKycStatusesFromAllowedSet()
    {
        var body = await _client.GetFromJsonAsync<PagedResponse<SignalProviderRequestDto>>("/api/dashboard/signalProviderRequests");

        Assert.NotNull(body);
        Assert.All(body.Items, r => Assert.Contains(r.KycStatus, AllowedKycStatuses));
    }

    // ── User Story 5: GET /api/dashboard/affiliateRequests ────────────────────────

    [Fact]
    public async Task GetAffiliateRequests_Returns200WithExactlyTenRecords()
    {
        var resp = await _client.GetAsync("/api/dashboard/affiliateRequests");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PagedResponse<AffiliateRequestDto>>();
        Assert.NotNull(body);
        Assert.Equal(10, body.Items.Count);
    }

    [Fact]
    public async Task GetAffiliateRequests_AllKycStatusesFromAllowedSet()
    {
        var body = await _client.GetFromJsonAsync<PagedResponse<AffiliateRequestDto>>("/api/dashboard/affiliateRequests");

        Assert.NotNull(body);
        Assert.All(body.Items, r => Assert.Contains(r.KycStatus, AllowedKycStatuses));
    }

    // ── Phase 8: Swagger ─────────────────────────────────────────────────────

    [Fact]
    public async Task SwaggerJson_ContainsAllFiveMockEndpoints()
    {
        var resp = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("paths", out var paths));

        Assert.True(paths.TryGetProperty("/api/dashboard/listOfUsers",           out _), "Missing /api/dashboard/listOfUsers");
        Assert.True(paths.TryGetProperty("/api/currentActiveUser",                out _), "Missing /api/currentActiveUser");
        Assert.True(paths.TryGetProperty("/api/dashboard/clientRequests",          out _), "Missing /api/dashboard/clientRequests");
        Assert.True(paths.TryGetProperty("/api/dashboard/signalProviderRequests",  out _), "Missing /api/dashboard/signalProviderRequests");
        Assert.True(paths.TryGetProperty("/api/dashboard/affiliateRequests",       out _), "Missing /api/dashboard/affiliateRequests");
    }
}

/// <summary>
/// Verifies that mock endpoints return 404 outside the Development environment (FR-011 / SC-006).
/// Uses IntegrationWebFactory which sets environment to "Testing" (not Development).
/// </summary>
public class MockNonDevTests : IClassFixture<IntegrationWebFactory>
{
    private readonly HttpClient _client;

    public MockNonDevTests(IntegrationWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_NonDevelopmentEnvironment_Returns404()
    {
        var resp = await _client.GetAsync("/api/dashboard/listOfUsers");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_NonDevelopmentEnvironment_Returns404()
    {
        var resp = await _client.GetAsync("/api/currentActiveUser");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
