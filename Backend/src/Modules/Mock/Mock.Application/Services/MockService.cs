namespace Mock.Application.Services;

/// <summary>
/// Provides static in-memory mock data for the Dashboard screen.
/// All data is deterministic — identical on every call.
/// </summary>
public class MockService : IMockService
{
    // ── User List ─────────────────────────────────────────────────────────────

    private static readonly List<UserDto> _users =
    [
        new("1",  "Carlos Silva",    "Signal Provider"),
        new("2",  "Ana Costa",       "Affiliate"),
        new("3",  "John Doe",        "Client"),
        new("4",  "Maria Santos",    "Client"),
        new("5",  "Pedro Oliveira",  "Affiliate"),
        new("6",  "Yuki Tanaka",     "Signal Provider"),
        new("7",  "Sofia Andrade",   "Client"),
        new("8",  "Marco Rossi",     "Signal Provider"),
        new("9",  "Li Wei",          "Affiliate"),
        new("10", "Emma Wilson",     "Client"),
    ];

    // ── Current Active User ───────────────────────────────────────────────────

    private static readonly CurrentUserDto _currentUser =
        new("1", "Carlos Silva", "CS", "Signal Provider");

    // ── Client Requests ───────────────────────────────────────────────────────

    private static readonly List<ClientRequestDto> _clientRequests =
    [
        new(new DateTime(2026, 4, 13, 10, 30, 0, DateTimeKind.Utc), "Jörg Müller",      12500.00m, "Alpha Growth",    "LIC-001"),
        new(new DateTime(2026, 4, 12, 14, 15, 0, DateTimeKind.Utc), "Alice Johnson",      8750.50m, "Beta Momentum",   "LIC-002"),
        new(new DateTime(2026, 4, 11,  9, 45, 0, DateTimeKind.Utc), "Chen Wei",          15000.00m, "Delta Scalper",   "LIC-003"),
        new(new DateTime(2026, 4, 10, 16, 00, 0, DateTimeKind.Utc), "Fatima Al-Hassan",   5200.75m, "Alpha Growth",    "LIC-001"),
        new(new DateTime(2026, 4,  9, 11, 30, 0, DateTimeKind.Utc), "James Okafor",       9800.00m, "Gamma Swing",     "LIC-004"),
        new(new DateTime(2026, 4,  8,  8, 20, 0, DateTimeKind.Utc), "Laura Fernández",    3400.25m, "Beta Momentum",   "LIC-002"),
        new(new DateTime(2026, 4,  7, 13, 10, 0, DateTimeKind.Utc), "Nguyen Van An",     21000.00m, "Delta Scalper",   "LIC-003"),
        new(new DateTime(2026, 4,  6, 17, 45, 0, DateTimeKind.Utc), "Priya Sharma",       6750.00m, "Epsilon Trend",   "LIC-005"),
        new(new DateTime(2026, 4,  5, 10, 00, 0, DateTimeKind.Utc), "David Kim",         11200.50m, "Alpha Growth",    "LIC-001"),
        new(new DateTime(2026, 4,  4, 15, 30, 0, DateTimeKind.Utc), "Aisha Nkosi",        4900.00m, "Gamma Swing",     "LIC-004"),
    ];

    // ── Signal Provider Requests ──────────────────────────────────────────────

    private static readonly List<SignalProviderRequestDto> _signalProviderRequests =
    [
        new(new DateTime(2026, 4, 13,  9, 00, 0, DateTimeKind.Utc), "Marco Rossi",      "Pending"),
        new(new DateTime(2026, 4, 12, 11, 45, 0, DateTimeKind.Utc), "Yuki Tanaka",      "Verified"),
        new(new DateTime(2026, 4, 11, 14, 30, 0, DateTimeKind.Utc), "Carlos Silva",     "Verified"),
        new(new DateTime(2026, 4, 10,  8, 15, 0, DateTimeKind.Utc), "Lena Fischer",     "Pending"),
        new(new DateTime(2026, 4,  9, 16, 00, 0, DateTimeKind.Utc), "Kwame Asante",     "Rejected"),
        new(new DateTime(2026, 4,  8, 10, 30, 0, DateTimeKind.Utc), "Hana Suzuki",      "Verified"),
        new(new DateTime(2026, 4,  7, 13, 45, 0, DateTimeKind.Utc), "Igor Petrov",      "Pending"),
        new(new DateTime(2026, 4,  6,  9, 20, 0, DateTimeKind.Utc), "Mia Bergström",    "Verified"),
        new(new DateTime(2026, 4,  5, 15, 10, 0, DateTimeKind.Utc), "Omar Khalid",      "Rejected"),
        new(new DateTime(2026, 4,  4, 11, 00, 0, DateTimeKind.Utc), "Valentina Cruz",   "Pending"),
    ];

    // ── Affiliate Requests ────────────────────────────────────────────────────

    private static readonly List<AffiliateRequestDto> _affiliateRequests =
    [
        new(new DateTime(2026, 4, 13,  8, 00, 0, DateTimeKind.Utc), "Sofia Andrade",    "Verified"),
        new(new DateTime(2026, 4, 12, 16, 20, 0, DateTimeKind.Utc), "Li Wei",           "Pending"),
        new(new DateTime(2026, 4, 11, 10, 45, 0, DateTimeKind.Utc), "Ana Costa",        "Verified"),
        new(new DateTime(2026, 4, 10, 14, 00, 0, DateTimeKind.Utc), "Pedro Oliveira",   "Pending"),
        new(new DateTime(2026, 4,  9,  9, 30, 0, DateTimeKind.Utc), "Chloe Martin",     "Rejected"),
        new(new DateTime(2026, 4,  8, 12, 15, 0, DateTimeKind.Utc), "Ravi Patel",       "Verified"),
        new(new DateTime(2026, 4,  7, 15, 45, 0, DateTimeKind.Utc), "Amara Diallo",     "Pending"),
        new(new DateTime(2026, 4,  6, 10, 10, 0, DateTimeKind.Utc), "Lukas Bauer",      "Verified"),
        new(new DateTime(2026, 4,  5, 17, 00, 0, DateTimeKind.Utc), "Nadia Ivanova",    "Rejected"),
        new(new DateTime(2026, 4,  4,  8, 30, 0, DateTimeKind.Utc), "Tariq Al-Rashid",  "Pending"),
    ];

    // ── IMockService ──────────────────────────────────────────────────────────

    public Task<PagedResponse<UserDto>> GetUsersAsync(string? searchText = null)
    {
        var filtered = string.IsNullOrWhiteSpace(searchText)
            ? _users
            : _users.Where(u => u.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult(PagedResponse<UserDto>.All(filtered));
    }

    public Task<CurrentUserDto> GetCurrentUserAsync()
        => Task.FromResult(_currentUser);

    public Task<PagedResponse<ClientRequestDto>> GetClientRequestsAsync()
        => Task.FromResult(PagedResponse<ClientRequestDto>.All(_clientRequests));

    public Task<PagedResponse<SignalProviderRequestDto>> GetSignalProviderRequestsAsync()
        => Task.FromResult(PagedResponse<SignalProviderRequestDto>.All(_signalProviderRequests));

    public Task<PagedResponse<AffiliateRequestDto>> GetAffiliateRequestsAsync()
        => Task.FromResult(PagedResponse<AffiliateRequestDto>.All(_affiliateRequests));
}
