namespace Tracking.Domain.Entities;

public class ConversionEvent : BaseEntity
{
    public long Id { get; set; }
    public int AffiliateId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string ConversionType { get; set; } = string.Empty;
    public DateTime ConvertedAt { get; set; }
}
