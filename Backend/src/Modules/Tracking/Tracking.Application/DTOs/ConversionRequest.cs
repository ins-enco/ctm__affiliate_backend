namespace Tracking.Application.DTOs;

public record ConversionRequest
{
    [Required] public string SessionId { get; init; } = null!;
    [Required] public string ConversionType { get; init; } = null!;
    public int? UserId { get; init; }
}
