namespace Tracking.Application.DTOs;

public record ConversionResult(bool IsAttributed, string? AffiliateCode, string ConversionType, string Message);
