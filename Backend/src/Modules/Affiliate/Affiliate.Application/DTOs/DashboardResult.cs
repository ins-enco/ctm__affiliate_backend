namespace Affiliate.Application.DTOs;

public record DashboardResult(
    string AffiliateName,
    string UniqueCode,
    int TotalClicks,
    int UniqueClicks,
    int Last7DayClicks,
    int CachedClickCount);
