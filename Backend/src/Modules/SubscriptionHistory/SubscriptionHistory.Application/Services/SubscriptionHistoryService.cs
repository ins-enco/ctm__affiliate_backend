namespace SubscriptionHistory.Application.Services;

public class SubscriptionHistoryService : ISubscriptionHistoryService
{
    private static readonly IReadOnlyList<SubscriptionHistoryItem> MockedData =
    [
        new(new DateTime(2026, 4, 13, 10, 30, 0), "Alice Tran",   "ACC-001", "Alpha Growth",    12500.00m, null,      "Subscribe"),
        new(new DateTime(2026, 4, 12, 15, 45, 0), "Bob Nguyen",   "ACC-002", "Beta Momentum",   8750.50m,  8900.00m,  "Unsubscribe"),
        new(new DateTime(2026, 4, 12, 09, 00, 0), "Charlie Le",   "ACC-003", "Gamma Scalper",   5000.00m,  null,      "Subscribe"),
        new(new DateTime(2026, 4, 11, 17, 20, 0), "Alice Tran",   "ACC-001", "Beta Momentum",   12000.00m, 12450.00m, "Unsubscribe"),
        new(new DateTime(2026, 4, 11, 11, 10, 0), "Diana Pham",   "ACC-004", "Alpha Growth",    9300.75m,  null,      "Subscribe"),
        new(new DateTime(2026, 4, 10, 14, 55, 0), "Bob Nguyen",   "ACC-002", "Delta Swing",     7200.00m,  null,      "Subscribe"),
        new(new DateTime(2026, 4, 10, 08, 30, 0), "Charlie Le",   "ACC-003", "Alpha Growth",    4800.00m,  4950.00m,  "Unsubscribe"),
        new(new DateTime(2026, 4, 09, 16, 00, 0), "Eve Hoang",    "ACC-005", "Gamma Scalper",   15000.00m, null,      "Subscribe"),
        new(new DateTime(2026, 4, 09, 10, 45, 0), "Diana Pham",   "ACC-004", "Delta Swing",     9100.00m,  9250.00m,  "Unsubscribe"),
        new(new DateTime(2026, 4, 08, 13, 30, 0), "Alice Tran",   "ACC-001", "Gamma Scalper",   11800.00m, null,      "Subscribe"),
        new(new DateTime(2026, 4, 08, 09, 15, 0), "Bob Nguyen",   "ACC-002", "Alpha Growth",    8000.00m,  8100.00m,  "Unsubscribe"),
        new(new DateTime(2026, 4, 07, 15, 00, 0), "Eve Hoang",    "ACC-005", "Beta Momentum",   14500.00m, 14750.00m, "Unsubscribe"),
        new(new DateTime(2026, 4, 07, 11, 20, 0), "Frank Vu",     "ACC-006", "Alpha Growth",    6500.00m,  null,      "Subscribe"),
        new(new DateTime(2026, 4, 06, 14, 10, 0), "Charlie Le",   "ACC-003", "Delta Swing",     5200.00m,  null,      "Subscribe"),
        new(new DateTime(2026, 4, 06, 08, 45, 0), "Diana Pham",   "ACC-004", "Beta Momentum",   9800.00m,  null,      "Subscribe"),
        new(new DateTime(2026, 4, 05, 17, 30, 0), "Frank Vu",     "ACC-006", "Gamma Scalper",   6300.00m,  6450.00m,  "Unsubscribe"),
        new(new DateTime(2026, 4, 05, 10, 00, 0), "Alice Tran",   "ACC-001", "Delta Swing",     12200.00m, 12350.00m, "Unsubscribe"),
        new(new DateTime(2026, 4, 04, 16, 20, 0), "Eve Hoang",    "ACC-005", "Alpha Growth",    14000.00m, null,      "Subscribe"),
        new(new DateTime(2026, 4, 04, 09, 50, 0), "Bob Nguyen",   "ACC-002", "Gamma Scalper",   7800.00m,  null,      "Subscribe"),
        new(new DateTime(2026, 4, 03, 14, 00, 0), "Frank Vu",     "ACC-006", "Beta Momentum",   6100.00m,  6200.00m,  "Unsubscribe"),
    ];

    public Task<PagedResponse<SubscriptionHistoryItem>> GetAsync(int? page, int? pageSize)
    {
        // Guard: invalid pagination values
        if (page.HasValue && page.Value < 1)
            throw new ArgumentException("Page number must be greater than 0.", nameof(page));

        if (pageSize.HasValue && pageSize.Value < 1)
            throw new ArgumentException("Page size must be greater than 0.", nameof(pageSize));

        // No pagination requested — return everything
        if (!page.HasValue && !pageSize.HasValue)
            return Task.FromResult(PagedResponse<SubscriptionHistoryItem>.All(MockedData));

        // At least one param provided — apply effective defaults
        int effectivePage     = page     ?? 1;
        int effectivePageSize = pageSize ?? 20;

        int skip = (effectivePage - 1) * effectivePageSize;
        var slice = MockedData.Skip(skip).Take(effectivePageSize).ToList();

        return Task.FromResult(
            PagedResponse<SubscriptionHistoryItem>.Paginated(slice, MockedData.Count, effectivePage, effectivePageSize));
    }
}
