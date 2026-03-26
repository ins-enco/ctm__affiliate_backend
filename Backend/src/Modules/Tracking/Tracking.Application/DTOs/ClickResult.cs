namespace Tracking.Application.DTOs;

public record ClickResult(bool IsUnique, string AffiliateCode, string Message, string? SessionId = null);
