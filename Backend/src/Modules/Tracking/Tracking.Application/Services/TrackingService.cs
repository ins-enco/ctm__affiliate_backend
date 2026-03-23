namespace Tracking.Application.Services;

public class TrackingService(
    TrackingDbContext db,
    IAffiliateLookupService affiliateLookup,
    IMemoryCache cache) : ITrackingService
{
    public async Task<ClickResult> RecordClickAsync(
        string affiliateCode, string? ipAddress, string? userAgent, string? existingSessionId)
    {
        var affiliate = await affiliateLookup.FindByCodeAsync(affiliateCode)
            ?? throw new KeyNotFoundException($"Affiliate code '{affiliateCode}' not found.");

        var (affiliateId, _) = affiliate;

        // Use existing cookie session ID or generate a new one
        var sessionId = existingSessionId
            ?? HashHelper.Sha256($"{ipAddress}{userAgent}{affiliateCode}");

        var alreadyExists = await db.ClickEvents
            .AnyAsync(e => e.AffiliateId == affiliateId && e.SessionId == sessionId);

        if (alreadyExists)
            return new ClickResult(false, affiliateCode, "Click already recorded for this session.");

        db.ClickEvents.Add(new ClickEvent
        {
            AffiliateId = affiliateId,
            SessionId = sessionId,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            ClickedAt = DateTime.UtcNow,
            IsUnique = true
        });
        await db.SaveChangesAsync();

        // Invalidate cached click count so dashboard reads fresh stats
        cache.Remove($"affiliate:clickcount:{affiliateId}");

        return new ClickResult(true, affiliateCode, "Click recorded.");
    }
}
