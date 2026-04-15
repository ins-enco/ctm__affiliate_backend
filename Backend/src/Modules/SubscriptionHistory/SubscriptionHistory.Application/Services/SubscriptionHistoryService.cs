namespace SubscriptionHistory.Application.Services;

public class SubscriptionHistoryService : ISubscriptionHistoryService
{
    private static readonly HashSet<string> AllowedOrderByFields =
    ["timestamp", "clientName", "accountNumber", "strategyName", "equityConnect"];

    private static readonly IReadOnlyList<SubscriptionHistoryItem> MockedData =
    [
        new(1,   new DateTime(2026, 4, 13, 10, 30, 0), "Jörg Müller",           "ACC-001", "Alpha Growth",                    12500.00m, null,      "Subscribe",   "Active"),
        new(2,   new DateTime(2026, 4, 12, 15, 45, 0), "Renée Dupont",          "ACC-002", "Beta Momentum",                   8750.50m,  8900.00m,  "Unsubscribe", "Approved"),
        new(3,   new DateTime(2026, 4, 12, 09, 00, 0), "李伟",                   "ACC-003", "Gamma Scalper",                   5000.00m,  null,      "Subscribe",   "Pending"),
        new(4,   new DateTime(2026, 4, 11, 17, 20, 0), "Hans Mueller",          "ACC-001", "Quantum Flux",                   12000.00m, 12450.00m, "Unsubscribe", "Inactive"),
        new(5,   new DateTime(2026, 4, 11, 11, 10, 0), "आरव पटेल",               "ACC-004", "Alpha Growth",                    9300.75m,  null,      "Subscribe",   "Active"),
        new(6,   new DateTime(2026, 4, 10, 14, 55, 0), "王伟",                   "ACC-002", "Neo Phoenix",                     7200.00m,  null,      "Subscribe",   "New"),
        new(7,   new DateTime(2026, 4, 10, 08, 30, 0), "Thierry Blanc",         "ACC-003", "Vortex Strategy",                 4800.00m,  4950.00m,  "Unsubscribe", "Terminated"),
        new(8,   new DateTime(2026, 4, 09, 16, 00, 0), "Eve Hoang",             "ACC-005", "Gamma Scalper",                  15000.00m, null,      "Subscribe",   "Active"),
        new(9,   new DateTime(2026, 4, 09, 10, 45, 0), "Diana Pham",            "ACC-004", "Delta Swing",                     9100.00m,  9250.00m,  "Unsubscribe", "Approved"),
        new(10,  new DateTime(2026, 4, 08, 13, 30, 0), BuildRandomEnglishName(100), "ACC-001", "Gamma Scalper",              11800.00m, null,      "Subscribe",   "Connecting"),
        new(11,  new DateTime(2026, 4, 08, 09, 15, 0), "Wolfgang Schmidt",      "ACC-002", "Alpha Growth",                    8000.00m,  8100.00m,  "Unsubscribe", "Withdraw"),
        new(12,  new DateTime(2026, 4, 07, 15, 00, 0), "Eve Hoang",             "ACC-005", "Horizon Wave",                   14500.00m, 14750.00m, "Unsubscribe", "Active"),
        new(13,  new DateTime(2026, 4, 07, 11, 20, 0), "Min Wang",              "ACC-006", "Crystal Peak",                    6500.00m,  null,      "Subscribe",   "Pending"),
        new(14,  new DateTime(2026, 4, 06, 14, 10, 0), "Zoë François",          "ACC-004", "Delta Swing",                     5200.00m,  null,      "Subscribe",   "Inactive"),
        new(15,  new DateTime(2026, 4, 06, 08, 45, 0), "Diana Phạm",            "ACC-004", "Beta Momentum",                   9800.00m,  null,      "Subscribe",   "Active"),
        new(16,  new DateTime(2026, 4, 05, 17, 30, 0), "Frank Vu",              "ACC-006", "Gamma Scalper",                   6300.00m,  6450.00m,  "Unsubscribe", "Terminated"),
        new(17,  new DateTime(2026, 4, 05, 10, 00, 0), "Alice Tran",            "ACC-001", "Delta Swing",                    12200.00m, 12350.00m, "Unsubscribe", "Approved"),
        new(18,  new DateTime(2026, 4, 04, 16, 20, 0), "Pierre Duval",          "ACC-005", "Sentinel Flow",                  14000.00m, null,      "Subscribe",   "New"),
        new(19,  new DateTime(2026, 4, 04, 09, 50, 0), "Bob Nguyen",            "ACC-002", "Nexus Prime",                     7800.00m,  null,      "Subscribe",   "Active"),
        new(20,  new DateTime(2026, 4, 03, 14, 00, 0), "Frank Vũ",              "ACC-006", BuildRandomStrategyName(100),      6100.00m,  6200.00m,  "Unsubscribe", "Pending"),
        new(21,  new DateTime(2026, 4, 02, 11, 25, 0), "Carlos López",          "ACC-007", "Alpha Growth",                    8900.00m,  null,      "Subscribe",   "Active"),
        new(22,  new DateTime(2026, 4, 01, 16, 40, 0), "Maria Garcia",          "ACC-008", "Beta Momentum",                  11200.00m, 11350.00m, "Unsubscribe", "Inactive"),
        new(23,  new DateTime(2026, 3, 31, 10, 15, 0), "João Silva",            "ACC-009", "Gamma Scalper",                   7650.00m,  null,      "Subscribe",   "Approved"),
        new(24,  new DateTime(2026, 3, 30, 14, 50, 0), "Svetlana Ivanov",       "ACC-010", "Delta Swing",                    13400.00m, 13550.00m, "Unsubscribe", "Terminated"),
        new(25,  new DateTime(2026, 3, 29, 09, 30, 0), BuildRandomEnglishName(100), "ACC-001", "Epsilon Trend",               6200.00m,  null,      "Subscribe",   "New"),
        new(26,  new DateTime(2026, 3, 28, 15, 20, 0), "Amara Okafor",          "ACC-002", "Zeta Wave",                      10100.00m, 10250.00m, "Unsubscribe", "Connecting"),
        new(27,  new DateTime(2026, 3, 27, 11, 45, 0), "Hassan Ahmed",          "ACC-003", "Alpha Growth",                    9500.00m,  null,      "Subscribe",   "Active"),
        new(28,  new DateTime(2026, 3, 26, 13, 35, 0), "Ingrid Larsson",        "ACC-004", "Beta Momentum",                   7100.00m,  7250.00m,  "Unsubscribe", "Pending"),
        new(29,  new DateTime(2026, 3, 25, 10, 10, 0), "Pavel Volkov",          "ACC-005", "Gamma Scalper",                  14200.00m, null,      "Subscribe",   "Approved"),
        new(30,  new DateTime(2026, 3, 24, 16, 55, 0), "Élena Petrov",          "ACC-006", BuildRandomStrategyName(100),      8800.00m,  9000.00m,  "Unsubscribe", "Withdraw"),
        new(31,  new DateTime(2026, 3, 23, 09, 20, 0), "Marcus Johnson",        "ACC-007", "Epsilon Trend",                   5500.00m,  null,      "Subscribe",   "Active"),
        new(32,  new DateTime(2026, 3, 22, 14, 05, 0), "Natasha Kozlov",        "ACC-008", "Zeta Wave",                      11800.00m, 12000.00m, "Unsubscribe", "Inactive"),
        new(33,  new DateTime(2026, 3, 21, 11, 30, 0), "Omar Hassan",           "ACC-009", "Alpha Growth",                    6900.00m,  null,      "Subscribe",   "Terminated"),
        new(34,  new DateTime(2026, 3, 20, 15, 45, 0), "Lucia Martino",         "ACC-010", "Beta Momentum",                   9300.00m,  9450.00m,  "Unsubscribe", "Active"),
        new(35,  new DateTime(2026, 3, 19, 10, 00, 0), "Dmitri Sokolov",        "ACC-001", "Gamma Scalper",                  12100.00m, null,      "Subscribe",   "Pending"),
        new(36,  new DateTime(2026, 3, 18, 16, 20, 0), "Victória Chen",         "ACC-002", "Delta Swing",                     7400.00m,  7600.00m,  "Unsubscribe", "New"),
        new(37,  new DateTime(2026, 3, 17, 12, 35, 0), "Klaus Mueller",         "ACC-003", "Epsilon Trend",                  10500.00m, null,      "Subscribe",   "Active"),
        new(38,  new DateTime(2026, 3, 16, 14, 10, 0), "Fatima Al-Rashid",      "ACC-004", "Zeta Wave",                       8200.00m,  8350.00m,  "Unsubscribe", "Connecting"),
        new(39,  new DateTime(2026, 3, 15, 09, 55, 0), "Bruno Rossi",           "ACC-005", "Alpha Growth",                   11600.00m, null,      "Subscribe",   "Approved"),
        new(40,  new DateTime(2026, 3, 14, 15, 30, 0), BuildRandomEnglishName(100), "ACC-006", "Beta Momentum",               6800.00m,  7000.00m,  "Unsubscribe", "Inactive"),
        new(41,  new DateTime(2026, 3, 13, 11, 15, 0), "Andre Santos",          "ACC-007", "Gamma Scalper",                   9700.00m,  null,      "Subscribe",   "Active"),
        new(42,  new DateTime(2026, 3, 12, 13, 50, 0), "Helga Fischer",         "ACC-008", "Delta Swing",                    13900.00m, 14050.00m, "Unsubscribe", "Terminated"),
        new(43,  new DateTime(2026, 3, 11, 10, 25, 0), "Rajesh Kumar",          "ACC-009", "Epsilon Trend",                   5300.00m,  null,      "Subscribe",   "Pending"),
        new(44,  new DateTime(2026, 3, 10, 15, 40, 0), "Giulia Rosso",          "ACC-010", "Zeta Wave",                      10900.00m, 11050.00m, "Unsubscribe", "Active"),
        new(45,  new DateTime(2026, 3, 09, 12, 00, 0), "Andréy Petrov",         "ACC-001", BuildRandomStrategyName(100),      8100.00m,  null,      "Subscribe",   "New"),
        new(46,  new DateTime(2026, 3, 08, 16, 15, 0), "Sofia Rossi",           "ACC-002", "Beta Momentum",                  12300.00m, 12450.00m, "Unsubscribe", "Approved"),
        new(47,  new DateTime(2026, 3, 07, 11, 50, 0), "Sergei Volkov",         "ACC-003", "Gamma Scalper",                   7200.00m,  null,      "Subscribe",   "Active"),
        new(48,  new DateTime(2026, 3, 06, 14, 35, 0), "Cristina Lopez",        "ACC-004", "Delta Swing",                     9950.00m,  10100.00m, "Unsubscribe", "Withdraw"),
        new(49,  new DateTime(2026, 3, 05, 10, 20, 0), "Mikhail Sokolov",       "ACC-005", "Epsilon Trend",                  11400.00m, null,      "Subscribe",   "Connecting"),
        new(50,  new DateTime(2026, 3, 04, 15, 05, 0), BuildRandomEnglishName(100), "ACC-006", "Zeta Wave",                   6600.00m,  6850.00m,  "Unsubscribe", "Inactive"),
        new(51,  new DateTime(2026, 3, 03, 12, 30, 0), "Ivan Petrov",           "ACC-007", "Alpha Growth",                    9200.00m,  null,      "Subscribe",   "Active"),
        new(52,  new DateTime(2026, 3, 02, 13, 45, 0), "Martina Bauer",         "ACC-008", "Beta Momentum",                   8500.00m,  8700.00m,  "Unsubscribe", "Pending"),
        new(53,  new DateTime(2026, 3, 01, 11, 10, 0), "Pavel Alexeev",         "ACC-009", "Gamma Scalper",                  13100.00m, null,      "Subscribe",   "Approved"),
        new(54,  new DateTime(2026, 2, 28, 15, 55, 0), "Katrin Schmidt",        "ACC-010", "Delta Swing",                     7800.00m,  8000.00m,  "Unsubscribe", "Terminated"),
        new(55,  new DateTime(2026, 2, 27, 10, 40, 0), "Alexéi Kozlov",         "ACC-001", BuildRandomStrategyName(100),     10700.00m, null,      "Subscribe",   "Active"),
        new(56,  new DateTime(2026, 2, 26, 14, 20, 0), "Simone Ferrari",        "ACC-002", "Zeta Wave",                       6400.00m,  6600.00m,  "Unsubscribe", "New"),
        new(57,  new DateTime(2026, 2, 25, 12, 50, 0), "Yuri Semenov",          "ACC-003", "Alpha Growth",                   11900.00m, null,      "Subscribe",   "Inactive"),
        new(58,  new DateTime(2026, 2, 24, 16, 35, 0), "Francesca Russo",       "ACC-004", "Beta Momentum",                   9600.00m,  9750.00m,  "Unsubscribe", "Active"),
        new(59,  new DateTime(2026, 2, 23, 11, 05, 0), "Oleg Smirnov",          "ACC-005", "Gamma Scalper",                   5900.00m,  null,      "Subscribe",   "Pending"),
        new(60,  new DateTime(2026, 2, 22, 15, 25, 0), BuildRandomEnglishName(100), "ACC-006", "Delta Swing",                12700.00m, 12850.00m, "Unsubscribe", "Connecting"),
        new(61,  new DateTime(2026, 2, 21, 13, 15, 0), "Viktor Volkov",         "ACC-007", "Epsilon Trend",                   8300.00m,  null,      "Subscribe",   "Approved"),
        new(62,  new DateTime(2026, 2, 20, 10, 50, 0), "Margherita Boni",       "ACC-008", "Zeta Wave",                      10300.00m, 10450.00m, "Unsubscribe", "Active"),
        new(63,  new DateTime(2026, 2, 19, 14, 40, 0), "Gennady Maximov",       "ACC-009", "Alpha Growth",                    7600.00m,  null,      "Subscribe",   "Withdraw"),
        new(64,  new DateTime(2026, 2, 18, 12, 25, 0), "Aurora Mancini",        "ACC-010", "Beta Momentum",                  11100.00m, 11250.00m, "Unsubscribe", "Terminated"),
        new(65,  new DateTime(2026, 2, 17, 11, 20, 0), "Stanísláv Orlov",       "ACC-001", BuildRandomStrategyName(100),      9400.00m,  null,      "Subscribe",   "Active"),
        new(66,  new DateTime(2026, 2, 16, 15, 45, 0), "Valentina Costa",       "ACC-002", "Delta Swing",                     6900.00m,  7100.00m,  "Unsubscribe", "Pending"),
        new(67,  new DateTime(2026, 2, 15, 10, 35, 0), "Kirill Sokolov",        "ACC-003", "Epsilon Trend",                  13300.00m, null,      "Subscribe",   "New"),
        new(68,  new DateTime(2026, 2, 14, 16, 10, 0), "Rosalia Gallo",         "ACC-004", "Zeta Wave",                       8600.00m,  8750.00m,  "Unsubscribe", "Active"),
        new(69,  new DateTime(2026, 2, 13, 12, 55, 0), "Leonid Petrov",         "ACC-005", "Alpha Growth",                   10200.00m, null,      "Subscribe",   "Approved"),
        new(70,  new DateTime(2026, 2, 12, 14, 30, 0), "Màrta Bellini",         "ACC-006", "Beta Momentum",                   7500.00m,  7700.00m,  "Unsubscribe", "Inactive"),
        new(71,  new DateTime(2026, 2, 11, 11, 40, 0), "Sergei Ivanov",         "ACC-007", "Gamma Scalper",                  12400.00m, null,      "Subscribe",   "Active"),
        new(72,  new DateTime(2026, 2, 10, 13, 20, 0), "Elena Caruso",          "ACC-008", "Delta Swing",                     9100.00m,  9250.00m,  "Unsubscribe", "Connecting"),
        new(73,  new DateTime(2026, 2, 09, 10, 10, 0), "Vladimir Kuznetsov",    "ACC-009", "Epsilon Trend",                   6100.00m,  null,      "Subscribe",   "Terminated"),
        new(74,  new DateTime(2026, 2, 08, 15, 50, 0), "Giuliana Rossi",        "ACC-010", "Zeta Wave",                      11500.00m, 11650.00m, "Unsubscribe", "Active"),
        new(75,  new DateTime(2026, 2, 07, 12, 25, 0), BuildRandomEnglishName(100), "ACC-001", "Alpha Growth",                8700.00m,  null,      "Subscribe",   "Pending"),
        new(76,  new DateTime(2026, 2, 06, 14, 05, 0), "Lucia Santoro",         "ACC-002", "Beta Momentum",                  10800.00m, 10950.00m, "Unsubscribe", "Approved"),
        new(77,  new DateTime(2026, 2, 05, 11, 35, 0), "Igor Smirnov",          "ACC-003", "Gamma Scalper",                   7300.00m,  null,      "Subscribe",   "New"),
        new(78,  new DateTime(2026, 2, 04, 16, 20, 0), "Antonella Rossi",       "ACC-004", "Delta Swing",                    12600.00m, 12750.00m, "Unsubscribe", "Active"),
        new(79,  new DateTime(2026, 2, 03, 10, 45, 0), "Anatoly Lebedev",       "ACC-005", "Epsilon Trend",                   5700.00m,  null,      "Subscribe",   "Inactive"),
        new(80,  new DateTime(2026, 2, 02, 13, 30, 0), "Sabrína De Lucas",      "ACC-006", BuildRandomStrategyName(100),      9800.00m,  10000.00m, "Unsubscribe", "Withdraw"),
        new(81,  new DateTime(2026, 2, 01, 11, 50, 0), "Evgeny Popov",          "ACC-007", "Alpha Growth",                   14100.00m, null,      "Subscribe",   "Active"),
        new(82,  new DateTime(2026, 1, 31, 15, 15, 0), "Serena Conte",          "ACC-008", "Beta Momentum",                   8900.00m,  9050.00m,  "Unsubscribe", "Pending"),
        new(83,  new DateTime(2026, 1, 30, 12, 40, 0), "Victor Orlov",          "ACC-009", "Gamma Scalper",                  11300.00m, null,      "Subscribe",   "Approved"),
        new(84,  new DateTime(2026, 1, 29, 14, 25, 0), "Fabiana Basile",        "ACC-010", "Delta Swing",                     6800.00m,  7000.00m,  "Unsubscribe", "Terminated"),
        new(85,  new DateTime(2026, 1, 28, 10, 55, 0), "Ígor Frolov",           "ACC-001", "Epsilon Trend",                  10400.00m, null,      "Subscribe",   "Active"),
        new(86,  new DateTime(2026, 1, 27, 16, 40, 0), "Monica Giordano",       "ACC-002", "Zeta Wave",                       7100.00m,  7300.00m,  "Unsubscribe", "New"),
        new(87,  new DateTime(2026, 1, 26, 11, 20, 0), "Igor Veselov",          "ACC-003", "Alpha Growth",                   13000.00m, null,      "Subscribe",   "Connecting"),
        new(88,  new DateTime(2026, 1, 25, 15, 05, 0), "Paola Fontana",         "ACC-004", "Beta Momentum",                   9500.00m,  9650.00m,  "Unsubscribe", "Active"),
        new(89,  new DateTime(2026, 1, 24, 13, 30, 0), "Boris Markov",          "ACC-005", "Gamma Scalper",                   8400.00m,  null,      "Subscribe",   "Inactive"),
        new(90,  new DateTime(2026, 1, 23, 10, 15, 0), BuildRandomEnglishName(100), "ACC-006", "Delta Swing",                12000.00m, 12150.00m, "Unsubscribe", "Approved"),
        new(91,  new DateTime(2026, 1, 22, 14, 50, 0), "Dmitri Volkov",         "ACC-007", "Epsilon Trend",                   6500.00m,  null,      "Subscribe",   "Active"),
        new(92,  new DateTime(2026, 1, 21, 12, 35, 0), "Emily Ferraro",         "ACC-008", "Zeta Wave",                      11200.00m, 11350.00m, "Unsubscribe", "Pending"),
        new(93,  new DateTime(2026, 1, 20, 11, 05, 0), "Michael Pavlov",        "ACC-009", "Alpha Growth",                    9000.00m,  null,      "Subscribe",   "Terminated"),
        new(94,  new DateTime(2026, 1, 19, 15, 45, 0), "Stefania Lombardi",     "ACC-010", "Beta Momentum",                   7700.00m,  7900.00m,  "Unsubscribe", "New"),
        new(95,  new DateTime(2026, 1, 18, 10, 25, 0), "Dènis Novikov",         "ACC-001", BuildRandomStrategyName(100),     10600.00m, null,      "Subscribe",   "Active"),
        new(96,  new DateTime(2026, 1, 17, 16, 10, 0), "Lucia Ferretti",        "ACC-002", "Delta Swing",                     8200.00m,  8400.00m,  "Unsubscribe", "Withdraw"),
        new(97,  new DateTime(2026, 1, 16, 13, 20, 0), "Vladimir Petrov",       "ACC-003", "Epsilon Trend",                  12900.00m, null,      "Subscribe",   "Approved"),
        new(98,  new DateTime(2026, 1, 15, 11, 50, 0), "Valentina Rossi",       "ACC-004", "Zeta Wave",                       6700.00m,  6900.00m,  "Unsubscribe", "Inactive"),
        new(99,  new DateTime(2026, 1, 14, 14, 35, 0), "Alexander Sokolov",     "ACC-005", "Alpha Growth",                   11700.00m, null,      "Subscribe",   "Active"),
        new(100, new DateTime(2026, 1, 13, 10, 05, 0), "Daniela Bernardi",      "ACC-006", "Beta Momentum",                   9700.00m,  9850.00m,  "Unsubscribe", "Connecting"),
    ];

    /// <summary>
    /// Retrieves subscription history with optional pagination, filtering, and sorting.
    /// </summary>
    /// <param name="page">The page number for pagination (1-based). If not provided, no pagination is applied.</param>
    /// <param name="pageSize">The number of records per page. If not provided, defaults to 20 when page is provided.</param>
    /// <param name="query">Optional search query to filter by clientName, accountNumber, or strategyName.</param>
    /// <param name="orderBy">Field to sort by. Allowed values: timestamp, clientName, accountNumber, strategyName, equityConnect. Defaults to timestamp.</param>
    /// <param name="orderDirection">Sort direction: asc or desc. Defaults to desc.</param>
    /// <returns>A paged response of subscription history items.</returns>
    public Task<PagedResponse<SubscriptionHistoryItem>> GetAsync(
        int? page,
        int? pageSize,
        string? query = null,
        string? statusFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? orderBy = null,
        string? orderDirection = null)
    {
        // Guard: invalid pagination values
        if (page.HasValue && page.Value < 1)
            throw new ArgumentException("Page number must be greater than 0.", nameof(page));

        if (pageSize.HasValue && pageSize.Value < 1)
            throw new ArgumentException("Page size must be greater than 0.", nameof(pageSize));

        var resolvedOrderBy = string.IsNullOrWhiteSpace(orderBy) ? "timestamp" : orderBy;
        if (!AllowedOrderByFields.Contains(resolvedOrderBy, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid orderBy. Allowed values: timestamp, clientName, accountNumber, strategyName, equityConnect.", nameof(orderBy));

        var resolvedOrderDirection = string.IsNullOrWhiteSpace(orderDirection) ? "desc" : orderDirection;
        if (!resolvedOrderDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
            && !resolvedOrderDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid orderDirection. Allowed values: asc, desc.", nameof(orderDirection));
        }

        var filtered = ApplyQuery(MockedData, query);
        filtered = ApplyQueryStatus(filtered, statusFilter);
        filtered = ApplyDateRange(filtered, fromDate, toDate);
        var ordered = ApplyOrdering(filtered, resolvedOrderBy, resolvedOrderDirection).ToList();

        // No pagination requested — return everything
        if (!page.HasValue && !pageSize.HasValue)
            return Task.FromResult(PagedResponse<SubscriptionHistoryItem>.All(ordered));

        // At least one param provided — apply effective defaults
        int effectivePage     = page     ?? 1;
        int effectivePageSize = pageSize ?? 20;

        int skip = (effectivePage - 1) * effectivePageSize;
        var slice = ordered.Skip(skip).Take(effectivePageSize).ToList();

        return Task.FromResult(
            PagedResponse<SubscriptionHistoryItem>.Paginated(slice, ordered.Count, effectivePage, effectivePageSize));
    }

    private IEnumerable<SubscriptionHistoryItem> ApplyQueryStatus(IEnumerable<SubscriptionHistoryItem> filtered, string? statusFilter)
    {
        if (string.IsNullOrWhiteSpace(statusFilter))
            return filtered;

        var term = statusFilter.Trim();
        return filtered.Where(x => x.Status.Equals(term, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<SubscriptionHistoryItem> ApplyDateRange(IEnumerable<SubscriptionHistoryItem> filtered, DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue)
            filtered = filtered.Where(x => x.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            filtered = filtered.Where(x => x.Timestamp <= toDate.Value);

        return filtered;
    }

    private static IEnumerable<SubscriptionHistoryItem> ApplyQuery(
        IEnumerable<SubscriptionHistoryItem> source,
        string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return source;

        var term = query.Trim();
        return source.Where(x =>
            x.ClientName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || x.AccountNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
            || x.StrategyName.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static IOrderedEnumerable<SubscriptionHistoryItem> ApplyOrdering(
        IEnumerable<SubscriptionHistoryItem> source,
        string orderBy,
        string orderDirection)
    {
        var isAscending = orderDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);

        return orderBy.ToLowerInvariant() switch
        {
            "timestamp" => isAscending ? source.OrderBy(x => x.Timestamp) : source.OrderByDescending(x => x.Timestamp),
            "clientname" => isAscending ? source.OrderBy(x => x.ClientName) : source.OrderByDescending(x => x.ClientName),
            "accountnumber" => isAscending ? source.OrderBy(x => x.AccountNumber) : source.OrderByDescending(x => x.AccountNumber),
            "strategyname" => isAscending ? source.OrderBy(x => x.StrategyName) : source.OrderByDescending(x => x.StrategyName),
            "equityconnect" => isAscending ? source.OrderBy(x => x.EquityConnect) : source.OrderByDescending(x => x.EquityConnect),
            _ => source.OrderByDescending(x => x.Timestamp)
        };
    }

    private static string BuildRandomEnglishName(int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return BuildRandomText(length, alphabet);
    }

    private static string BuildRandomStrategyName(int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return BuildRandomText(length, alphabet);
    }

    private static string BuildRandomText(int length, string alphabet)
    {
        var chars = new char[length];

        for (int i = 0; i < length; i++)
            chars[i] = alphabet[(i * 37 + 17) % alphabet.Length];

        return new string(chars);
    }
}
