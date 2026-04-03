using System.ComponentModel.DataAnnotations;

namespace CopyTradeMarketApi.Shared.Validation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PasswordFieldAttribute(
    int minLength = 8,
    bool requireUppercase = true,
    bool requireDigit = true,
    bool requireSpecialChar = true) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string password || string.IsNullOrEmpty(password))
            return ValidationResult.Success; // defer to [Required]

        var errors = new List<string>();

        if (password.Length < minLength)
            errors.Add($"{validationContext.DisplayName} must be at least {minLength} characters");

        if (requireUppercase && !password.Any(char.IsUpper))
            errors.Add($"{validationContext.DisplayName} must contain at least one uppercase letter");

        if (requireDigit && !password.Any(char.IsDigit))
            errors.Add($"{validationContext.DisplayName} must contain at least one digit");

        if (requireSpecialChar && !password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add($"{validationContext.DisplayName} must contain at least one special character");

        return errors.Count == 0
            ? ValidationResult.Success
            : new ValidationResult(string.Join("|", errors));
    }
}
