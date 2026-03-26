namespace Tracking.Application.Services;

public class TrackingService(
    TrackingDbContext db,
    IAffiliateLookupService affiliateLookup,
    ICacheService cache) : ITrackingService
{
    private static readonly TimeSpan AffiliateCacheTtl = TimeSpan.FromMinutes(10);

    // Returns a monthly bucket string included in the session hash.
    // Same IP+UA+code in a different month → different hash → new unique click.
    // Override in tests to control time without injecting a clock abstraction.
    protected virtual string GetAttributionBucket() =>
        DateTime.UtcNow.ToString("yyyy-MM");

    public async Task<ClickResult> RecordClickAsync(
        string affiliateCode, string? ipAddress, string? userAgent, string? existingSessionId)
    {
        var affiliate = await cache.GetOrCreateAsync(
                $"affiliate:code:{affiliateCode}",
                () => affiliateLookup.FindByCodeAsync(affiliateCode),
                AffiliateCacheTtl)
            ?? throw new KeyNotFoundException($"Affiliate code '{affiliateCode}' not found.");

        var (affiliateId, _) = affiliate;

        var sessionId = existingSessionId
            ?? HashHelper.Sha256($"{ipAddress}{userAgent}{affiliateCode}{GetAttributionBucket()}");

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
            return new ClickResult(true, affiliateCode, "Click recorded.", sessionId);
        }
        catch (DbUpdateException)
        {
            // Duplicate session caught by DB unique index on (AffiliateId, SessionId)
            return new ClickResult(false, affiliateCode, "Click already recorded for this session.", sessionId);
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
            var affiliate = await cache.GetOrCreateAsync(
                $"affiliate:id:{click.AffiliateId}",
                () => affiliateLookup.FindByIdAsync(click.AffiliateId),
                AffiliateCacheTtl);
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
}
