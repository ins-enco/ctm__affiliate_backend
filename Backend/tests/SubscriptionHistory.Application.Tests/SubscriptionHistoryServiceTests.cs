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
}
