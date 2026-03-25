namespace Tracking.Domain.Entities;

public class ClickEvent : BaseEntity
{
    public long Id { get; set; }
    public int AffiliateId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ClickedAt { get; set; }
}
