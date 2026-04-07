namespace Auth.Application.DTOs;

public record RegisterRequest : IValidatableObject
{
    [Required] public UserInformationDto UserInformation { get; init; } = null!;
    [Required][PasswordField] public string Password { get; init; } = null!;
    [Required] public string ConfirmPassword { get; init; } = null!;
    public string? SessionId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Password != null && ConfirmPassword != null && Password != ConfirmPassword)
            yield return new ValidationResult("Passwords do not match.", new[] { "ConfirmPassword" });
    }
}
