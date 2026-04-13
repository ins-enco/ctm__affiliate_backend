namespace CopyTradeMarketApi.Shared.Tests.Responses;

public class PagedResponseTests
{
    // --- All() factory ---

    [Fact]
    public void All_WithItems_ReturnsTotalCountEqualToItemsCount()
    {
        var items = new List<string> { "a", "b", "c" };

        var result = PagedResponse<string>.All(items);

        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public void All_WithItems_ReturnsNullPaginationFields()
    {
        var items = new List<string> { "a", "b", "c" };

        var result = PagedResponse<string>.All(items);

        Assert.Null(result.Page);
        Assert.Null(result.PageSize);
        Assert.Null(result.TotalPages);
    }

    [Fact]
    public void All_WithEmptyList_ReturnsZeroTotalCount()
    {
        var items = new List<string>();

        var result = PagedResponse<string>.All(items);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    // --- Paginated() factory ---

    [Fact]
    public void Paginated_WithFullPage_ReturnsCorrectMetadata()
    {
        var items = new List<string> { "a", "b", "c", "d", "e" };

        var result = PagedResponse<string>.Paginated(items, totalCount: 20, page: 1, pageSize: 5);

        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(4, result.TotalPages);
        Assert.Equal(20, result.TotalCount);
    }

    [Fact]
    public void Paginated_ComputesTotalPagesWithCeiling()
    {
        var items = new List<string> { "a", "b", "c", "d", "e" };

        var result = PagedResponse<string>.Paginated(items, totalCount: 21, page: 1, pageSize: 5);

        Assert.Equal(5, result.TotalPages); // ceil(21/5) = 5
    }

    [Fact]
    public void Paginated_WithTotalCountZero_ReturnsTotalPagesZero()
    {
        var items = new List<string>();

        var result = PagedResponse<string>.Paginated(items, totalCount: 0, page: 1, pageSize: 10);

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void Paginated_WithEmptyPage_ReturnsEmptyItemsButCorrectTotalCount()
    {
        var items = new List<string>();

        var result = PagedResponse<string>.Paginated(items, totalCount: 20, page: 3, pageSize: 10);

        Assert.Empty(result.Items);
        Assert.Equal(20, result.TotalCount);
    }
}
