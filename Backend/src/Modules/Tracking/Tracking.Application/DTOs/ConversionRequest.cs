namespace Tracking.Application.DTOs;

public record ConversionRequest(string SessionId, string ConversionType, int? UserId);
