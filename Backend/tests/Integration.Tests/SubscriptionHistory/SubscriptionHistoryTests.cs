using CopyTradeMarketApi.Shared.Responses;
using SubscriptionHistory.Application.DTOs;

namespace Integration.Tests.SubscriptionHistory;

/// <summary>
/// Integration tests for GET /api/subscription-history.
/// Covers: all-records (US1) and paginated (US2) scenarios.
/// </summary>
public class SubscriptionHistoryTests : IClassFixture<IntegrationWebFactory>
{
    private readonly HttpClient _client;

    public SubscriptionHistoryTests(IntegrationWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── User Story 1 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSubscriptionHistory_NoPagination_Returns200WithAllRecords()
    {
        var resp = await _client.GetAsync("/api/subscription-history");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PagedResponse<SubscriptionHistoryItem>>();
        Assert.NotNull(body);
        Assert.Equal(20, body.Items.Count);
        Assert.Equal(20, body.TotalCount);
        Assert.Null(body.Page);
        Assert.Null(body.PageSize);
        Assert.Null(body.TotalPages);
    }

    // ── User Story 2 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSubscriptionHistory_WithValidPagination_Returns200WithMetadata()
    {
        var resp = await _client.GetAsync("/api/subscription-history?page=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PagedResponse<SubscriptionHistoryItem>>();
        Assert.NotNull(body);
        Assert.Equal(5, body.Items.Count);
        Assert.Equal(20, body.TotalCount);
        Assert.Equal(1, body.Page);
        Assert.Equal(5, body.PageSize);
        Assert.Equal(4, body.TotalPages);
    }

    [Fact]
    public async Task GetSubscriptionHistory_WithZeroPage_Returns400ProblemDetails()
    {
        var resp = await _client.GetAsync("/api/subscription-history?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("detail", out _) || body.TryGetProperty("title", out _));
    }

    [Fact]
    public async Task GetSubscriptionHistory_WithZeroPageSize_Returns400ProblemDetails()
    {
        var resp = await _client.GetAsync("/api/subscription-history?page=1&pageSize=0");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("detail", out _) || body.TryGetProperty("title", out _));
    }

    [Fact]
    public async Task GetSubscriptionHistory_WithPageBeyondTotal_Returns200EmptyItems()
    {
        var resp = await _client.GetAsync("/api/subscription-history?page=99&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PagedResponse<SubscriptionHistoryItem>>();
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.Equal(20, body.TotalCount);
    }
}
