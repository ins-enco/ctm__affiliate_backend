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
        Assert.True(result.Items.Count >= 5, $"Expected ≥5 users, got {result.Items.Count}");
        Assert.True(result.TotalCount >= 5);
    }

    [Fact]
    public async Task GetUsersAsync_CoversAllThreeRoles()
    {
        var result = await _service.GetUsersAsync();

        Assert.Contains(result.Items, u => u.Role == "Client");
        Assert.Contains(result.Items, u => u.Role == "Signal Provider");
        Assert.Contains(result.Items, u => u.Role == "Affiliate");
    }

    [Fact]
    public async Task GetUsersAsync_AllRolesFromAllowedSet()
    {
        var result = await _service.GetUsersAsync();

        Assert.All(result.Items, u => Assert.Contains(u.Role, AllowedRoles));
    }

    [Fact]
    public async Task GetUsersAsync_AllRecordsHaveNonEmptyNameAndId()
    {
        var result = await _service.GetUsersAsync();

        Assert.All(result.Items, u =>
        {
            Assert.False(string.IsNullOrWhiteSpace(u.Id));
            Assert.False(string.IsNullOrWhiteSpace(u.Name));
        });
    }

    [Fact]
    public async Task GetUsersAsync_NonPaginated()
    {
        var result = await _service.GetUsersAsync();

        Assert.Null(result.Page);
        Assert.Null(result.PageSize);
        Assert.Null(result.TotalPages);
        Assert.Equal(result.Items.Count, result.TotalCount);
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

        Assert.False(string.IsNullOrWhiteSpace(result.Id));
        Assert.False(string.IsNullOrWhiteSpace(result.Name));
    }

    // ── User Story 3: Client Requests ─────────────────────────────────────────

    [Fact]
    public async Task GetClientRequestsAsync_ReturnsExactlyTenRecords()
    {
        var result = await _service.GetClientRequestsAsync();

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(10, result.TotalCount);
    }

    [Fact]
    public async Task GetClientRequestsAsync_AllEquityValuesArePositive()
    {
        var result = await _service.GetClientRequestsAsync();

        Assert.All(result.Items, r => Assert.True(r.Equity > 0, $"Equity must be > 0, got {r.Equity}"));
    }

    [Fact]
    public async Task GetClientRequestsAsync_AllFieldsNonEmpty()
    {
        var result = await _service.GetClientRequestsAsync();

        Assert.All(result.Items, r =>
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

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(10, result.TotalCount);
    }

    [Fact]
    public async Task GetSignalProviderRequestsAsync_AllKycStatusesFromAllowedSet()
    {
        var result = await _service.GetSignalProviderRequestsAsync();

        Assert.All(result.Items, r => Assert.Contains(r.KycStatus, AllowedKycStatuses));
    }

    [Fact]
    public async Task GetSignalProviderRequestsAsync_AllNamesNonEmpty()
    {
        var result = await _service.GetSignalProviderRequestsAsync();

        Assert.All(result.Items, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
    }

    // ── User Story 5: Affiliate Requests ─────────────────────────────────────

    [Fact]
    public async Task GetAffiliateRequestsAsync_ReturnsExactlyTenRecords()
    {
        var result = await _service.GetAffiliateRequestsAsync();

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(10, result.TotalCount);
    }

    [Fact]
    public async Task GetAffiliateRequestsAsync_AllKycStatusesFromAllowedSet()
    {
        var result = await _service.GetAffiliateRequestsAsync();

        Assert.All(result.Items, r => Assert.Contains(r.KycStatus, AllowedKycStatuses));
    }

    [Fact]
    public async Task GetAffiliateRequestsAsync_AllNamesNonEmpty()
    {
        var result = await _service.GetAffiliateRequestsAsync();

        Assert.All(result.Items, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
    }
}
