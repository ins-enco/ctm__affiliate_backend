namespace Tracking.Application.DTOs;

public record ClickConversionItem(
    string SessionId,
    DateTime ClickedAt,
    string ConversionType,
    DateTime ConvertedAt,
    int? UserId);
