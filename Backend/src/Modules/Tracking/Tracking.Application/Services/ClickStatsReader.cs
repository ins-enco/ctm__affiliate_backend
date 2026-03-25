namespace Tracking.Application.Services;

public class ClickStatsReader(TrackingDbContext db) : IClickStatsReader
{
    public async Task<ClickStats> GetAsync(int affiliateId)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);

        var total     = await db.ClickEvents
            .Apply(new ClicksByAffiliateSpecification(affiliateId))
            .CountAsync();
        var unique    = total;
        var last7     = await db.ClickEvents
            .Apply(new RecentClicksSpecification(affiliateId, cutoff))
            .CountAsync();
        var converted = await db.ClickEvents
            .Apply(new ClickWithConversionSpecification(affiliateId, db.ConversionEvents))
            .CountAsync();

        return new ClickStats(total, unique, last7, converted);
    }
}
