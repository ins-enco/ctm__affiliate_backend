namespace Tracking.Application.Services;

public class ClickStatsReader(TrackingDbContext db) : IClickStatsReader
{
    public async Task<ClickStats> GetAsync(int affiliateId)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);

        var total = await db.ClickEvents.CountAsync(e => e.AffiliateId == affiliateId);
        var unique = await db.ClickEvents.CountAsync(e => e.AffiliateId == affiliateId && e.IsUnique);
        var last7 = await db.ClickEvents.CountAsync(e => e.AffiliateId == affiliateId && e.IsUnique && e.ClickedAt >= cutoff);

        return new ClickStats(total, unique, last7);
    }
}
