namespace SubscriptionHistory.Application.Services;

public interface ISubscriptionHistoryService
{
    Task<PagedResponse<SubscriptionHistoryItem>> GetAsync(
        int? page,
        int? pageSize,
        string? query = null,
        string? orderBy = null,
        string? orderDirection = null);
}
