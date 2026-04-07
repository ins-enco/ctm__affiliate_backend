namespace Auth.Domain.Entities;

public class User : BaseEntity
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; } = false;
    public UserInformation? Information { get; set; }
    public ICollection<EmailVerificationToken> VerificationTokens { get; set; } = [];
}
