namespace SubscriptionHistory.Application.Tests;

public class SubscriptionHistoryServiceTests
{
    private readonly SubscriptionHistoryService _service = new();

    // ── User Story 1 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_WithNoPagination_ReturnsAllRecords()
    {
        var result = await _service.GetAsync(null, null);

        Assert.Equal(20, result.Items.Count);
        Assert.Equal(20, result.TotalCount);
        Assert.Null(result.Page);
        Assert.Null(result.PageSize);
        Assert.Null(result.TotalPages);
    }

    // ── User Story 2 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_WithPageAndPageSize_ReturnsCorrectSlice()
    {
        var result = await _service.GetAsync(1, 5);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(20, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(4, result.TotalPages);
    }

    [Fact]
    public async Task GetAsync_WithPageBeyondLast_ReturnsEmptyItems()
    {
        var result = await _service.GetAsync(99, 10);

        Assert.Empty(result.Items);
        Assert.Equal(20, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetAsync_WithPageSizeOnly_DefaultsPageToOne()
    {
        var result = await _service.GetAsync(null, 5);

        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task GetAsync_WithPageOnly_DefaultsPageSizeToTwenty()
    {
        var result = await _service.GetAsync(1, null);

        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task GetAsync_WithZeroPage_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAsync(0, 10));
    }

    [Fact]
    public async Task GetAsync_WithZeroPageSize_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAsync(1, 0));
    }

    // ── User Story 3 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_WithQuery_FiltersByClientAccountOrStrategy()
    {
        var result = await _service.GetAsync(null, null, query: "Alice");

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, item =>
            Assert.True(
                item.ClientName.Contains("Alice", StringComparison.OrdinalIgnoreCase)
                || item.AccountNumber.Contains("Alice", StringComparison.OrdinalIgnoreCase)
                || item.StrategyName.Contains("Alice", StringComparison.OrdinalIgnoreCase)));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public async Task GetAsync_WithVeryLongQuery_DoesNotThrowAndReturnsEmptyWhenNoMatch(int queryLength)
    {
        var query = new string('x', queryLength);

        var result = await _service.GetAsync(null, null, query: query);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    // ── User Story 4 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_WithOrderByClientName_DefaultsToDescending()
    {
        var result = await _service.GetAsync(null, null, orderBy: "clientName");

        Assert.NotEmpty(result.Items);
        var names = result.Items.Select(x => x.ClientName).ToList();
        var expected = names.OrderByDescending(x => x).ToList();
        Assert.Equal(expected, names);
    }

    [Fact]
    public async Task GetAsync_WithOrderDirectionOnly_AppliesToDefaultTimestamp()
    {
        var result = await _service.GetAsync(null, null, orderDirection: "asc");

        Assert.NotEmpty(result.Items);
        var timestamps = result.Items.Select(x => x.Timestamp).ToList();
        var expected = timestamps.OrderBy(x => x).ToList();
        Assert.Equal(expected, timestamps);
    }

    [Fact]
    public async Task GetAsync_WithInvalidOrderBy_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAsync(null, null, orderBy: "unknown"));
    }

    [Fact]
    public async Task GetAsync_WithInvalidOrderDirection_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAsync(null, null, orderDirection: "up"));
    }
}
