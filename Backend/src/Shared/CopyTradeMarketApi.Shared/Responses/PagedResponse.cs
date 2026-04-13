namespace CopyTradeMarketApi.Shared.Responses;

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int? Page,
    int? PageSize,
    int? TotalPages
)
{
    /// <summary>
    /// Creates a non-paginated response containing all provided items.
    /// Page, PageSize, and TotalPages are null in the response.
    /// </summary>
    public static PagedResponse<T> All(IReadOnlyList<T> items) =>
        new(items, items.Count, null, null, null);

    /// <summary>
    /// Creates a paginated response for a specific page slice.
    /// Computes TotalPages from totalCount and pageSize.
    /// </summary>
    public static PagedResponse<T> Paginated(
        IReadOnlyList<T> items,
        int totalCount,
        int page,
        int pageSize) =>
        new(
            items,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling((double)totalCount / pageSize)
        );
}
