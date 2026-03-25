namespace Tracking.Application.Services;

// High-traffic implementation of ITrackingService — designed for KOL livestream spikes.
// Differences from TrackingService.RecordClickAsync:
//   1. Affiliate lookup is cached (avoids a DB read on every click)
//   2. Pre-insert AnyAsync check removed — relies on DB unique index instead
//   3. Duplicate sessions detected via DbUpdateException (unique constraint violation)
// RecordConversionAsync is identical to TrackingService — no pressure expected there.
public class HighTrafficClickService(
    TrackingDbContext db,
    IAffiliateLookupService affiliateLookup,
    ICacheService cache) : ITrackingService
{
    private static readonly TimeSpan AffiliateCacheTtl = TimeSpan.FromMinutes(10);

    public async Task<ClickResult> RecordClickAsync(
        string affiliateCode, string? ipAddress, string? userAgent, string? existingSessionId)
    {
        var affiliate = await GetAffiliateCachedAsync(affiliateCode)
            ?? throw new KeyNotFoundException($"Affiliate code '{affiliateCode}' not found.");

        var (affiliateId, _) = affiliate;

        var sessionId = existingSessionId
            ?? HashHelper.Sha256($"{ipAddress}{userAgent}{affiliateCode}");

        try
        {
            db.ClickEvents.Add(new ClickEvent
            {
                AffiliateId = affiliateId,
                SessionId = sessionId,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                ClickedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            cache.Remove($"affiliate:clickcount:{affiliateId}");
            return new ClickResult(true, affiliateCode, "Click recorded.");
        }
        catch (DbUpdateException)
        {
            // Unique constraint (AffiliateId, SessionId) — duplicate session, skip silently
            return new ClickResult(false, affiliateCode, "Click already recorded for this session.");
        }
    }

    public async Task<ConversionResult> RecordConversionAsync(ConversionRequest request)
    {
        var validTypes = new[] { "Registration", "Deposit" };
        if (!validTypes.Contains(request.ConversionType))
            throw new InvalidOperationException($"Invalid conversion type '{request.ConversionType}'. Must be Registration or Deposit.");

        var alreadyConverted = await db.ConversionEvents
            .Apply(new ConversionBySessionAndTypeSpecification(request.SessionId, request.ConversionType))
            .AnyAsync();

        if (alreadyConverted)
            throw new ConflictException($"A {request.ConversionType} conversion has already been recorded for this session.");

        var click = await db.ClickEvents
            .Apply(new LatestClickBySessionSpecification(request.SessionId))
            .FirstOrDefaultAsync();

        string? affiliateCode = null;
        if (click is not null)
        {
            var affiliate = await GetAffiliateByIdCachedAsync(click.AffiliateId);
            affiliateCode = affiliate?.uniqueCode;
        }

        db.ConversionEvents.Add(new ConversionEvent
        {
            AffiliateId = click?.AffiliateId ?? 0,
            SessionId = request.SessionId,
            UserId = request.UserId,
            ConversionType = request.ConversionType,
            ConvertedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return click is not null
            ? new ConversionResult(true, affiliateCode, request.ConversionType, "Conversion recorded and attributed.")
            : new ConversionResult(false, null, request.ConversionType, "Conversion recorded but not attributed — no matching click found.");
    }

    private Task<(int affiliateId, string uniqueCode)?> GetAffiliateCachedAsync(string affiliateCode)
        => cache.GetOrCreateAsync(
            $"affiliate:code:{affiliateCode}",
            () => affiliateLookup.FindByCodeAsync(affiliateCode),
            AffiliateCacheTtl);

    private Task<(int affiliateId, string uniqueCode)?> GetAffiliateByIdCachedAsync(int affiliateId)
        => cache.GetOrCreateAsync(
            $"affiliate:id:{affiliateId}",
            () => affiliateLookup.FindByIdAsync(affiliateId),
            AffiliateCacheTtl);
}
