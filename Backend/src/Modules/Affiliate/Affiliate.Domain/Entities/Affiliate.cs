namespace Affiliate.Domain.Entities;

public class Affiliate : BaseEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UniqueCode { get; set; } = string.Empty;
    public int ClickCount { get; set; } = 0;
}
