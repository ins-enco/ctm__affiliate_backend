using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CopyTradeMarketApi.Shared.Validation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class StrictEmailFieldAttribute : ValidationAttribute
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string email || string.IsNullOrWhiteSpace(email))
            return ValidationResult.Success; // defer to [Required]

        return EmailRegex.IsMatch(email.Trim())
            ? ValidationResult.Success
            : new ValidationResult($"{validationContext.DisplayName} must be a valid email address");
    }
}
