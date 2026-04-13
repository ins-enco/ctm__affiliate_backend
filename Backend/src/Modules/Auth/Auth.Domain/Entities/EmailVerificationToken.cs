namespace Auth.Domain.Entities;

public class EmailVerificationToken : BaseEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public User User { get; set; } = null!;
}
