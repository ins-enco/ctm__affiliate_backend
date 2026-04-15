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
        var resp = await _client.GetAsync("/api/mock/users");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.NotNull(body);
        Assert.True(body.Count >= 5, $"Expected ≥5 users, got {body.Count}");
    }

    [Fact]
    public async Task GetUsers_CoversAllThreeRoles()
    {
        var body = await _client.GetFromJsonAsync<List<UserDto>>("/api/mock/users");

        Assert.NotNull(body);
        Assert.Contains(body, u => u.Role == "Client");
        Assert.Contains(body, u => u.Role == "Signal Provider");
        Assert.Contains(body, u => u.Role == "Affiliate");
    }

    [Fact]
    public async Task GetUsers_AllRolesFromAllowedSet()
    {
        var body = await _client.GetFromJsonAsync<List<UserDto>>("/api/mock/users");

        Assert.NotNull(body);
        Assert.All(body, u => Assert.Contains(u.Role, AllowedRoles));
    }

    // ── User Story 2: GET /api/mock/current-user ──────────────────────────────

    [Fact]
    public async Task GetCurrentUser_Returns200WithSingleObject()
    {
        var resp = await _client.GetAsync("/api/mock/current-user");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetCurrentUser_AbbreviationIsExactlyTwoChars()
    {
        var body = await _client.GetFromJsonAsync<CurrentUserDto>("/api/mock/current-user");

        Assert.NotNull(body);
        Assert.Equal(2, body.Abbreviation.Length);
    }

    [Fact]
    public async Task GetCurrentUser_RoleIsFromAllowedSet()
    {
        var body = await _client.GetFromJsonAsync<CurrentUserDto>("/api/mock/current-user");

        Assert.NotNull(body);
        Assert.Contains(body.Role, AllowedRoles);
    }

    // ── User Story 3: GET /api/mock/client-requests ───────────────────────────

    [Fact]
    public async Task GetClientRequests_Returns200WithExactlyTenRecords()
    {
        var resp = await _client.GetAsync("/api/mock/client-requests");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<List<ClientRequestDto>>();
        Assert.NotNull(body);
        Assert.Equal(10, body.Count);
    }

    [Fact]
    public async Task GetClientRequests_AllEquityValuesArePositive()
    {
        var body = await _client.GetFromJsonAsync<List<ClientRequestDto>>("/api/mock/client-requests");

        Assert.NotNull(body);
        Assert.All(body, r => Assert.True(r.Equity > 0));
    }

    // ── User Story 4: GET /api/mock/signal-provider-requests ─────────────────

    [Fact]
    public async Task GetSignalProviderRequests_Returns200WithExactlyTenRecords()
    {
        var resp = await _client.GetAsync("/api/mock/signal-provider-requests");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<List<SignalProviderRequestDto>>();
        Assert.NotNull(body);
        Assert.Equal(10, body.Count);
    }

    [Fact]
    public async Task GetSignalProviderRequests_AllKycStatusesFromAllowedSet()
    {
        var body = await _client.GetFromJsonAsync<List<SignalProviderRequestDto>>("/api/mock/signal-provider-requests");

        Assert.NotNull(body);
        Assert.All(body, r => Assert.Contains(r.KycStatus, AllowedKycStatuses));
    }

    // ── User Story 5: GET /api/mock/affiliate-requests ────────────────────────

    [Fact]
    public async Task GetAffiliateRequests_Returns200WithExactlyTenRecords()
    {
        var resp = await _client.GetAsync("/api/mock/affiliate-requests");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<List<AffiliateRequestDto>>();
        Assert.NotNull(body);
        Assert.Equal(10, body.Count);
    }

    [Fact]
    public async Task GetAffiliateRequests_AllKycStatusesFromAllowedSet()
    {
        var body = await _client.GetFromJsonAsync<List<AffiliateRequestDto>>("/api/mock/affiliate-requests");

        Assert.NotNull(body);
        Assert.All(body, r => Assert.Contains(r.KycStatus, AllowedKycStatuses));
    }

    // ── Phase 8: Swagger ─────────────────────────────────────────────────────

    [Fact]
    public async Task SwaggerJson_ContainsAllFiveMockEndpoints()
    {
        var resp = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("paths", out var paths));

        Assert.True(paths.TryGetProperty("/api/mock/users",                      out _), "Missing /api/mock/users");
        Assert.True(paths.TryGetProperty("/api/mock/current-user",               out _), "Missing /api/mock/current-user");
        Assert.True(paths.TryGetProperty("/api/mock/client-requests",            out _), "Missing /api/mock/client-requests");
        Assert.True(paths.TryGetProperty("/api/mock/signal-provider-requests",   out _), "Missing /api/mock/signal-provider-requests");
        Assert.True(paths.TryGetProperty("/api/mock/affiliate-requests",         out _), "Missing /api/mock/affiliate-requests");
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
        var resp = await _client.GetAsync("/api/mock/users");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_NonDevelopmentEnvironment_Returns404()
    {
        var resp = await _client.GetAsync("/api/mock/current-user");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
