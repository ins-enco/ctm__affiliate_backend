namespace SubscriptionHistory.Application.Services;

public interface ISubscriptionHistoryService
{
    Task<PagedResponse<SubscriptionHistoryItem>> GetAsync(
        int? page,
        int? pageSize,
        string? query = null,
        string? statusFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? orderBy = null,
        string? orderDirection = null);
}
