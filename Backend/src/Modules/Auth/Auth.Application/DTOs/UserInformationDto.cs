namespace Auth.Application.DTOs;

public record UserInformationDto
{
    [Required][MaxLength(50)]    public string FirstName   { get; init; } = null!;
    [Required][MaxLength(50)]    public string LastName    { get; init; } = null!;
    [Required][StrictEmailField] public string Email       { get; init; } = null!;
    [Required][PhoneCodeField]   public string PhoneCode   { get; init; } = null!;
    [Required][PhoneNumberField] public string PhoneNumber { get; init; } = null!;
    [Required][LanguageField]    public string Language    { get; init; } = null!;
}
