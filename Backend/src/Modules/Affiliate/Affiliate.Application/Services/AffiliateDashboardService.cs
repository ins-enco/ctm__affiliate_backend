namespace Affiliate.Application.Services;

public class AffiliateDashboardService(
    AffiliateDbContext db,
    IClickStatsReader clickStatsReader,
    ICacheService cache) : IAffiliateDashboardService
{
    public async Task<DashboardResult> GetDashboardAsync(int affiliateId)
    {
        var affiliate = await db.Affiliates.Apply(new AffiliateByIdSpecification(affiliateId)).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Affiliate not found.");

        var stats = await clickStatsReader.GetAsync(affiliateId);

        var cacheKey = $"affiliate:clickcount:{affiliateId}";
        var cachedCount = await cache.GetOrCreateAsync(
            cacheKey,
            () => Task.FromResult<int?>(stats.TotalClicks),
            TimeSpan.FromMinutes(5));

        return new DashboardResult(
            affiliate.Name,
            affiliate.UniqueCode,
            stats.TotalClicks,
            stats.UniqueClicks,
            stats.Last7DayClicks,
            stats.ConvertedClicks,
            cachedCount ?? stats.TotalClicks);
    }
}
