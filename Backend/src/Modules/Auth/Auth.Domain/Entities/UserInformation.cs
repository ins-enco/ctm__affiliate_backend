namespace Auth.Domain.Entities;

public class UserInformation : BaseEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}
