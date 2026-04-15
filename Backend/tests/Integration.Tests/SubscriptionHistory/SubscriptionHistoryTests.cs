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
        Assert.Equal(100, body.Items.Count);
        Assert.Equal(100, body.TotalCount);
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
        Assert.Equal(100, body.TotalCount);
        Assert.Equal(1, body.Page);
        Assert.Equal(5, body.PageSize);
        Assert.Equal(20, body.TotalPages);
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
        Assert.Equal(100, body.TotalCount);
    }

    // ── User Story 3 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSubscriptionHistory_WithQueryFilter_ReturnsMatchingRows()
    {
        var resp = await _client.GetAsync("/api/subscription-history?query=Alice");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PagedResponse<SubscriptionHistoryItem>>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.Items);
        Assert.All(body.Items, item =>
            Assert.True(
                item.ClientName.Contains("Alice", StringComparison.OrdinalIgnoreCase)
                || item.AccountNumber.Contains("Alice", StringComparison.OrdinalIgnoreCase)
                || item.StrategyName.Contains("Alice", StringComparison.OrdinalIgnoreCase)));
    }

    // ── User Story 4 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSubscriptionHistory_WithOrderByAndDirection_ReturnsSortedRows()
    {
        var resp = await _client.GetAsync("/api/subscription-history?orderBy=clientName&orderDirection=asc");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PagedResponse<SubscriptionHistoryItem>>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.Items);

        var names = body.Items.Select(x => x.ClientName).ToList();
        var expected = names.OrderBy(x => x).ToList();
        Assert.Equal(expected, names);
    }

    [Fact]
    public async Task GetSubscriptionHistory_WithInvalidOrderBy_Returns400ProblemDetails()
    {
        var resp = await _client.GetAsync("/api/subscription-history?orderBy=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("detail", out _) || body.TryGetProperty("title", out _));
    }

    [Fact]
    public async Task GetSubscriptionHistory_WithInvalidOrderDirection_Returns400ProblemDetails()
    {
        var resp = await _client.GetAsync("/api/subscription-history?orderDirection=up");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("detail", out _) || body.TryGetProperty("title", out _));
    }

    // ── Phase 7: Swagger ──────────────────────────────────────────────────────

    [Fact]
    public async Task SwaggerJson_ContainsSubscriptionHistoryOperationWithAllQueryParameters()
    {
        var resp = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("paths", out var paths));

        var operation = paths.GetProperty("/api/subscription-history").GetProperty("get");
        var parameters = operation.GetProperty("parameters");
        var names = parameters.EnumerateArray()
            .Select(p => p.GetProperty("name").GetString())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("query", names);
        Assert.Contains("statusFilter", names);
        Assert.Contains("fromDate", names);
        Assert.Contains("toDate", names);
        Assert.Contains("orderBy", names);
        Assert.Contains("orderDirection", names);
        Assert.Contains("page", names);
        Assert.Contains("pageSize", names);
    }
}
