namespace Mock.Application.Tests;

public class MockServiceTests
{
    private readonly MockService _service = new();

    private static readonly string[] AllowedRoles    = ["Client", "Signal Provider", "Affiliate"];
    private static readonly string[] AllowedKycStatuses = ["Pending", "Verified", "Rejected"];

    // ── User Story 1: User List ────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_ReturnsAtLeastFiveUsers()
    {
        var result = await _service.GetUsersAsync();

        Assert.NotNull(result);
        Assert.True(result.Count >= 5, $"Expected ≥5 users, got {result.Count}");
    }

    [Fact]
    public async Task GetUsersAsync_CoversAllThreeRoles()
    {
        var result = await _service.GetUsersAsync();

        Assert.Contains(result, u => u.Role == "Client");
        Assert.Contains(result, u => u.Role == "Signal Provider");
        Assert.Contains(result, u => u.Role == "Affiliate");
    }

    [Fact]
    public async Task GetUsersAsync_AllRolesFromAllowedSet()
    {
        var result = await _service.GetUsersAsync();

        Assert.All(result, u => Assert.Contains(u.Role, AllowedRoles));
    }

    [Fact]
    public async Task GetUsersAsync_AllRecordsHaveNonEmptyNameAndPositiveId()
    {
        var result = await _service.GetUsersAsync();

        Assert.All(result, u =>
        {
            Assert.True(u.Id > 0);
            Assert.False(string.IsNullOrWhiteSpace(u.Name));
        });
    }

    // ── User Story 2: Current Active User ─────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsNonNull()
    {
        var result = await _service.GetCurrentUserAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetCurrentUserAsync_AbbreviationIsExactlyTwoChars()
    {
        var result = await _service.GetCurrentUserAsync();

        Assert.Equal(2, result.Abbreviation.Length);
    }

    [Fact]
    public async Task GetCurrentUserAsync_RoleIsFromAllowedSet()
    {
        var result = await _service.GetCurrentUserAsync();

        Assert.Contains(result.Role, AllowedRoles);
    }

    [Fact]
    public async Task GetCurrentUserAsync_HasNonEmptyNameAndPositiveId()
    {
        var result = await _service.GetCurrentUserAsync();

        Assert.True(result.Id > 0);
        Assert.False(string.IsNullOrWhiteSpace(result.Name));
    }

    // ── User Story 3: Client Requests ─────────────────────────────────────────

    [Fact]
    public async Task GetClientRequestsAsync_ReturnsExactlyTenRecords()
    {
        var result = await _service.GetClientRequestsAsync();

        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task GetClientRequestsAsync_AllEquityValuesArePositive()
    {
        var result = await _service.GetClientRequestsAsync();

        Assert.All(result, r => Assert.True(r.Equity > 0, $"Equity must be > 0, got {r.Equity}"));
    }

    [Fact]
    public async Task GetClientRequestsAsync_AllFieldsNonEmpty()
    {
        var result = await _service.GetClientRequestsAsync();

        Assert.All(result, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Name));
            Assert.False(string.IsNullOrWhiteSpace(r.Strategy));
            Assert.False(string.IsNullOrWhiteSpace(r.StrategyLicense));
        });
    }

    // ── User Story 4: Signal Provider Requests ────────────────────────────────

    [Fact]
    public async Task GetSignalProviderRequestsAsync_ReturnsExactlyTenRecords()
    {
        var result = await _service.GetSignalProviderRequestsAsync();

        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task GetSignalProviderRequestsAsync_AllKycStatusesFromAllowedSet()
    {
        var result = await _service.GetSignalProviderRequestsAsync();

        Assert.All(result, r => Assert.Contains(r.KycStatus, AllowedKycStatuses));
    }

    [Fact]
    public async Task GetSignalProviderRequestsAsync_AllNamesNonEmpty()
    {
        var result = await _service.GetSignalProviderRequestsAsync();

        Assert.All(result, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
    }

    // ── User Story 5: Affiliate Requests ─────────────────────────────────────

    [Fact]
    public async Task GetAffiliateRequestsAsync_ReturnsExactlyTenRecords()
    {
        var result = await _service.GetAffiliateRequestsAsync();

        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task GetAffiliateRequestsAsync_AllKycStatusesFromAllowedSet()
    {
        var result = await _service.GetAffiliateRequestsAsync();

        Assert.All(result, r => Assert.Contains(r.KycStatus, AllowedKycStatuses));
    }

    [Fact]
    public async Task GetAffiliateRequestsAsync_AllNamesNonEmpty()
    {
        var result = await _service.GetAffiliateRequestsAsync();

        Assert.All(result, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
    }
}
