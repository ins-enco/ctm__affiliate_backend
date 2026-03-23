namespace Tracking.Application.Services;

public interface ITrackingService
{
    Task<ClickResult> RecordClickAsync(string affiliateCode, string? ipAddress, string? userAgent, string? existingSessionId);
    Task<ConversionResult> RecordConversionAsync(ConversionRequest request);
}
