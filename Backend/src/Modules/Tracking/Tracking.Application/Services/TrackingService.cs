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
            .Apply(new ClickByAffiliateAndSessionSpecification(affiliateId, sessionId))
            .AnyAsync();

        if (alreadyExists)
            return new ClickResult(false, affiliateCode, "Click already recorded for this session.");

        db.ClickEvents.Add(new ClickEvent
        {
            AffiliateId = affiliateId,
            SessionId = sessionId,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            ClickedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        // Invalidate cached click count so dashboard reads fresh stats
        cache.Remove($"affiliate:clickcount:{affiliateId}");

        return new ClickResult(true, affiliateCode, "Click recorded.");
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

        // Attribute to the affiliate that owns this session's click
        var click = await db.ClickEvents
            .Apply(new LatestClickBySessionSpecification(request.SessionId))
            .FirstOrDefaultAsync();

        string? affiliateCode = null;

        if (click is not null)
        {
            var affiliate = await affiliateLookup.FindByIdAsync(click.AffiliateId);
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
