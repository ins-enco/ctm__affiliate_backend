using Affiliate.Application.DTOs;

namespace Affiliate.Application.Services;

public class AffiliateDashboardService(
    AffiliateDbContext db,
    IClickStatsReader clickStatsReader,
    IMemoryCache cache) : IAffiliateDashboardService
{
    public async Task<DashboardResult> GetDashboardAsync(int affiliateId)
    {
        var affiliate = await db.Affiliates.FirstOrDefaultAsync(a => a.Id == affiliateId)
            ?? throw new KeyNotFoundException("Affiliate not found.");

        var stats = await clickStatsReader.GetAsync(affiliateId);

        var cacheKey = $"affiliate:clickcount:{affiliateId}";
        var cachedCount = cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return stats.TotalClicks;
        });

        return new DashboardResult(
            affiliate.Name,
            affiliate.UniqueCode,
            stats.TotalClicks,
            stats.UniqueClicks,
            stats.Last7DayClicks,
            cachedCount);
    }
}
